//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

First of all class contruction comes in two flavors precise and 'beforeFieldInit'. In C# you get the former
if you declare an explicit class constructor method and the later if you declaratively initialize static
fields. Precise class construction guarentees that the .cctor is run precisely before the first access to any
method or field of the class. 'beforeFieldInit' semantics guarentees only that the .cctor will be run some
time before the first static field access (note that calling methods (static or insance) or accessing
instance fields does not cause .cctors to be run).

Next you need to know that there are two kinds of code generation that can happen in the JIT: appdomain
neutral and appdomain specialized. The difference between these two kinds of code is how statics are handled.
For appdomain specific code, the address of a particular static variable is embeded in the code. This makes
it usable only for one appdomain (since every appdomain gets a own copy of its statics). Appdomain neutral
code calls a helper that looks up static variables off of a thread local variable. Thus the same code can be
used by mulitple appdomains in the same process.  

Generics also introduce a similar issue. Code for generic classes might be specialised for a particular set
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
that a .ctor always preceed a call to an instance methods. This break down when

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

The first 4 options are mutially exclusive 

    * CORINFO_FLG_HELPER If the field has this set, then the JIT must call getFieldHelper and call the
        returned helper with the object ref (for an instance field) and a fieldDesc. Note that this should be
        able to handle ANY field so to get a JIT up quickly, it has the option of using helper calls for all
        field access (and skip the complexity below). Note that for statics it is assumed that you will
        alwasy ask for the ADDRESSS helper and to the fetch in the JIT.

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

    * <NONE> This is a normal static field. Its address in in memory is determined by getFieldAddress. (see
        also CORINFO_FLG_STATIC_IN_HEAP).


This last field can modify any of the cases above except CORINFO_FLG_HELPER

CORINFO_FLG_STATIC_IN_HEAP This is currently only used for static fields of value classes. If the field has
this set then after computing what would normally be the field, what you actually get is a object poitner
(that must be reported to the GC) to a boxed version of the value. Thus the actual field address is computed
by addr = (*addr+sizeof(OBJECTREF))

Instance fields

    * CORINFO_FLG_HELPER This is used if the class is MarshalByRef, which means that the object might be a
        proxyt to the real object in some other appdomain or process. If the field has this set, then the JIT
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
                            JIT's responcibility to do the fetch or set. 

-------------------------------------------------------------------------------

TODO: Talk about initializing strutures before use 


*******************************************************************************
*/

#ifndef _COR_INFO_H_
#define _COR_INFO_H_

#include <corhdr.h>
#include <specstrings.h>

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
// #JITEEVersionIdentifier
//
// This GUID represents the version of the JIT/EE interface. Any time the interface between the JIT and
// the EE changes (by adding or removing methods to any interface shared between them), this GUID should
// be changed. This is the identifier verified by ICorJitCompiler::getVersionIdentifier().
//
// You can use "uuidgen.exe -s" to generate this value.
//
// **** NOTE TO INTEGRATORS:
//
// If there is a merge conflict here, because the version changed in two different places, you must
// create a **NEW** GUID, not simply choose one or the other!
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////

#if !defined(SELECTANY)
    #define SELECTANY extern __declspec(selectany)
#endif

// COR_JIT_EE_VERSION is a #define that specifies a JIT-EE version, but on a less granular basis than the GUID.
// The #define is intended to be used on a per-product basis. That is, for each release that we support a JIT
// CTP build, we'll update the COR_JIT_EE_VERSION. The GUID must change any time any part of the interface changes.
//
// COR_JIT_EE_VERSION is set, by convention, to a number related to the the product number. So, 460 is .NET 4.60.
// 461 would indicate .NET 4.6.1. Etc.
//
// Note that the EE should always build with the most current (highest numbered) version. Only the JIT will
// potentially build with a lower version number. In that case, the COR_JIT_EE_VERSION will be specified in the
// CTP JIT build project, such as ctpjit.nativeproj.

#if !defined(COR_JIT_EE_VERSION)
#define COR_JIT_EE_VERSION 999999999    // This means we'll take everything in the interface
#endif

#if COR_JIT_EE_VERSION > 460

// Update this one
SELECTANY const GUID JITEEVersionIdentifier = { /* f7be09f3-9ca7-42fd-b0ca-f97c0499f5a3 */
    0xf7be09f3,
    0x9ca7,
    0x42fd,
    {0xb0, 0xca, 0xf9, 0x7c, 0x04, 0x99, 0xf5, 0xa3}
};

#else

// ************ Leave this one alone ***************
// We need it to build a .NET 4.6 compatible JIT for the RyuJIT CTP releases
SELECTANY const GUID JITEEVersionIdentifier = { /* 9110edd8-8fc3-4e3d-8ac9-12555ff9be9c */
    0x9110edd8,
    0x8fc3,
    0x4e3d,
    { 0x8a, 0xc9, 0x12, 0x55, 0x5f, 0xf9, 0xbe, 0x9c }
};

#endif

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// END JITEEVersionIdentifier
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////

#if COR_JIT_EE_VERSION > 460

// For System V on the CLR type system number of registers to pass in and return a struct is the same.
// The CLR type system allows only up to 2 eightbytes to be passed in registers. There is no SSEUP classification types.
#define CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS   2 
#define CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_RETURN_IN_REGISTERS 2
#define CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS       16

// System V struct passing
// The Classification types are described in the ABI spec at http://www.x86-64.org/documentation/abi.pdf
enum SystemVClassificationType : unsigned __int8
{
    SystemVClassificationTypeUnknown            = 0,
    SystemVClassificationTypeStruct             = 1,
    SystemVClassificationTypeNoClass            = 2,
    SystemVClassificationTypeMemory             = 3,
    SystemVClassificationTypeInteger            = 4,
    SystemVClassificationTypeIntegerReference   = 5,
    SystemVClassificationTypeSSE                = 6,
    // SystemVClassificationTypeSSEUp           = Unused, // Not supported by the CLR.
    // SystemVClassificationTypeX87             = Unused, // Not supported by the CLR.
    // SystemVClassificationTypeX87Up           = Unused, // Not supported by the CLR.
    // SystemVClassificationTypeComplexX87      = Unused, // Not supported by the CLR.
    SystemVClassificationTypeMAX = 7,
};

// Represents classification information for a struct.
struct SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR
{
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR()
    {
        Initialize();
    }

    bool                        passedInRegisters; // Whether the struct is passable/passed (this includes struct returning) in registers.
    unsigned __int8             eightByteCount;    // Number of eightbytes for this struct.
    SystemVClassificationType   eightByteClassifications[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS]; // The eightbytes type classification.
    unsigned __int8             eightByteSizes[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];           // The size of the eightbytes (an eightbyte could include padding. This represents the no padding size of the eightbyte).
    unsigned __int8             eightByteOffsets[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];         // The start offset of the eightbytes (in bytes).


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

    // Members
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

#endif // COR_JIT_EE_VERSION

// CorInfoHelpFunc defines the set of helpers (accessed via the ICorDynamicInfo::getHelperFtn())
// These helpers can be called by native code which executes in the runtime.
// Compilers can emit calls to these helpers.
//
// The signatures of the helpers are below (see RuntimeHelperArgumentCheck)
//
//  NOTE: CorInfoHelpFunc is closely related to MdilHelpFunc!!!
//  
//  - changing the order of jit helper ordinals works fine
//  However:
//  - adding a jit helpers requires usually the addition of a corresponding MdilHelper
//  - removing a jit helper (or changing its arguments) should be done only sparingly
//    and needs discussion with an "MDIL person".
//  Please have a look also at the comment prepending the definition of MdilHelpFunc
//

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

    CORINFO_HELP_NEW_CROSSCONTEXT,  // cross context new object
    CORINFO_HELP_NEWFAST,
    CORINFO_HELP_NEWSFAST,          // allocator for small, non-finalizer, non-array object
    CORINFO_HELP_NEWSFAST_ALIGN8,   // allocator for small, non-finalizer, non-array object, 8 byte aligned
    CORINFO_HELP_NEW_MDARR,         // multi-dim array helper (with or without lower bounds)
    CORINFO_HELP_NEWARR_1_DIRECT,   // helper for any one dimensional array creation
    CORINFO_HELP_NEWARR_1_OBJ,      // optimized 1-D object arrays
    CORINFO_HELP_NEWARR_1_VC,       // optimized 1-D value class arrays
    CORINFO_HELP_NEWARR_1_ALIGN8,   // like VC, but aligns the array start

    CORINFO_HELP_STRCNS,            // create a new string literal
    CORINFO_HELP_STRCNS_CURRENT_MODULE, // create a new string literal from the current module (used by NGen code)

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

    CORINFO_HELP_BOX,
    CORINFO_HELP_BOX_NULLABLE,      // special form of boxing for Nullable<T>
    CORINFO_HELP_UNBOX,
    CORINFO_HELP_UNBOX_NULLABLE,    // special form of unboxing for Nullable<T>
    CORINFO_HELP_GETREFANY,         // Extract the byref from a TypedReference, checking that it is the expected type

    CORINFO_HELP_ARRADDR_ST,        // assign to element of object array with type-checking
    CORINFO_HELP_LDELEMA_REF,       // does a precise type comparision and returns address

    /* Exceptions */

    CORINFO_HELP_THROW,             // Throw an exception object
    CORINFO_HELP_RETHROW,           // Rethrow the currently active exception
    CORINFO_HELP_USER_BREAKPOINT,   // For a user program to break to the debugger
    CORINFO_HELP_RNGCHKFAIL,        // array bounds check failed
    CORINFO_HELP_OVERFLOW,          // throw an overflow exception
    CORINFO_HELP_THROWDIVZERO,      // throw a divide by zero exception
#if COR_JIT_EE_VERSION > 460
    CORINFO_HELP_THROWNULLREF,      // throw a null reference exception
#endif // COR_JIT_EE_VERSION

    CORINFO_HELP_INTERNALTHROW,     // Support for really fast jit
    CORINFO_HELP_VERIFICATION,      // Throw a VerificationException
    CORINFO_HELP_SEC_UNMGDCODE_EXCPT, // throw a security unmanaged code exception
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

    /* Security callout support */
    
    CORINFO_HELP_SECURITY_PROLOG,   // Required if CORINFO_FLG_SECURITYCHECK is set, or CORINFO_FLG_NOSECURITYWRAP is not set
    CORINFO_HELP_SECURITY_PROLOG_FRAMED, // Slow version of CORINFO_HELP_SECURITY_PROLOG. Used for instrumentation.

    CORINFO_HELP_METHOD_ACCESS_CHECK, // Callouts to runtime security access checks
    CORINFO_HELP_FIELD_ACCESS_CHECK,
    CORINFO_HELP_CLASS_ACCESS_CHECK,

    CORINFO_HELP_DELEGATE_SECURITY_CHECK, // Callout to delegate security transparency check

     /* Verification runtime callout support */

    CORINFO_HELP_VERIFICATION_RUNTIME_CHECK, // Do a Demand for UnmanagedCode permission at runtime

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

    CORINFO_HELP_GETSTATICFIELDADDR_CONTEXT,    // Helper for context-static fields
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
    CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG,// determine a type/field/method handle at run-time, with IBC logging
    CORINFO_HELP_RUNTIMEHANDLE_CLASS,    // determine a type/field/method handle at run-time
    CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG,// determine a type/field/method handle at run-time, with IBC logging

    // These helpers are required for MDIL backward compatibility only. They are not used by current JITed code.
    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_OBSOLETE, // Convert from a TypeHandle (native structure pointer) to RuntimeTypeHandle at run-time
    CORINFO_HELP_METHODDESC_TO_RUNTIMEMETHODHANDLE_OBSOLETE, // Convert from a MethodDesc (native structure pointer) to RuntimeMethodHandle at run-time
    CORINFO_HELP_FIELDDESC_TO_RUNTIMEFIELDHANDLE_OBSOLETE, // Convert from a FieldDesc (native structure pointer) to RuntimeFieldHandle at run-time

    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time
    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time, the type may be null
    CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD, // Convert from a MethodDesc (native structure pointer) to RuntimeMethodHandle at run-time
    CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD, // Convert from a FieldDesc (native structure pointer) to RuntimeFieldHandle at run-time

    CORINFO_HELP_VIRTUAL_FUNC_PTR,      // look up a virtual method at run-time
    //CORINFO_HELP_VIRTUAL_FUNC_PTR_LOG,  // look up a virtual method at run-time, with IBC logging

    // Not a real helpers. Instead of taking handle arguments, these helpers point to a small stub that loads the handle argument and calls the static helper.
    CORINFO_HELP_READYTORUN_NEW,
    CORINFO_HELP_READYTORUN_NEWARR_1,
    CORINFO_HELP_READYTORUN_ISINSTANCEOF,
    CORINFO_HELP_READYTORUN_CHKCAST,
    CORINFO_HELP_READYTORUN_STATIC_BASE,
    CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR,

#if COR_JIT_EE_VERSION > 460
    CORINFO_HELP_READYTORUN_DELEGATE_CTOR,
#else
    #define CORINFO_HELP_READYTORUN_DELEGATE_CTOR CORINFO_HELP_EE_PRESTUB
#endif // COR_JIT_EE_VERSION

#ifdef REDHAWK
    // these helpers are arbitrary since we don't have any relation to the actual CLR corinfo.h.
    CORINFO_HELP_PINVOKE,               // transition to preemptive mode for a pinvoke, frame in EAX
    CORINFO_HELP_PINVOKE_2,             // transition to preemptive mode for a pinvoke, frame in ESI / R10
    CORINFO_HELP_PINVOKE_RETURN,        // return to cooperative mode from a pinvoke
    CORINFO_HELP_REVERSE_PINVOKE,       // transition to cooperative mode for a callback from native
    CORINFO_HELP_REVERSE_PINVOKE_RETURN,// return to preemptive mode to return to native from managed
    CORINFO_HELP_REGISTER_MODULE,       // module load notification
    CORINFO_HELP_CREATECOMMANDLINE,     // get the command line from the system and return it for Main
    CORINFO_HELP_VSD_INITIAL_TARGET,    // all VSD indirection cells initially point to this function
    CORINFO_HELP_NEW_FINALIZABLE,       // allocate finalizable object
    CORINFO_HELP_SHUTDOWN,              // called when Main returns from a managed executable
    CORINFO_HELP_CHECKARRAYSTORE,       // checks that an array element assignment is of the right type
    CORINFO_HELP_CHECK_VECTOR_ELEM_ADDR,// does a precise type check on the array element type
    CORINFO_HELP_FLT2INT_OVF,           // checked float->int conversion
    CORINFO_HELP_FLT2LNG,               // float->long conversion
    CORINFO_HELP_FLT2LNG_OVF,           // checked float->long conversion
    CORINFO_HELP_FLTREM_REV,            // Bartok helper for float remainder - uses reversed param order from CLR helper
    CORINFO_HELP_DBLREM_REV,            // Bartok helper for double remainder - uses reversed param order from CLR helper
    CORINFO_HELP_HIJACKFORGCSTRESS,     // this helper hijacks the caller for GC stress
    CORINFO_HELP_INIT_GCSTRESS,         // this helper initializes the runtime for GC stress
    CORINFO_HELP_SUPPRESS_GCSTRESS,     // disables gc stress
    CORINFO_HELP_UNSUPPRESS_GCSTRESS,   // re-enables gc stress
    CORINFO_HELP_THROW_INTRA,           // Throw an exception object to a hander within the method
    CORINFO_HELP_THROW_INTER,           // Throw an exception object to a hander within the caller
    CORINFO_HELP_THROW_ARITHMETIC,      // Throw the classlib-defined arithmetic exception
    CORINFO_HELP_THROW_DIVIDE_BY_ZERO,  // Throw the classlib-defined divide by zero exception
    CORINFO_HELP_THROW_INDEX,           // Throw the classlib-defined index out of range exception
    CORINFO_HELP_THROW_OVERFLOW,        // Throw the classlib-defined overflow exception
    CORINFO_HELP_EHJUMP_SCALAR,         // Helper to jump to a handler in a different method for EH dispatch.
    CORINFO_HELP_EHJUMP_OBJECT,         // Helper to jump to a handler in a different method for EH dispatch.
    CORINFO_HELP_EHJUMP_BYREF,          // Helper to jump to a handler in a different method for EH dispatch.
    CORINFO_HELP_EHJUMP_SCALAR_GCSTRESS,// Helper to jump to a handler in a different method for EH dispatch.
    CORINFO_HELP_EHJUMP_OBJECT_GCSTRESS,// Helper to jump to a handler in a different method for EH dispatch.
    CORINFO_HELP_EHJUMP_BYREF_GCSTRESS, // Helper to jump to a handler in a different method for EH dispatch.

    // Bartok emits code with destination in ECX rather than EDX and only ever uses EDX as the reference
    // register. It also only ever specifies the checked version.
    CORINFO_HELP_CHECKED_ASSIGN_REF_EDX, // EDX hold GC ptr, want do a 'mov [ECX], EDX' and inform GC
#endif // REDHAWK

    CORINFO_HELP_EE_PRESTUB,            // Not real JIT helper. Used in native images.

    CORINFO_HELP_EE_PRECODE_FIXUP,      // Not real JIT helper. Used for Precode fixup in native images.
    CORINFO_HELP_EE_PINVOKE_FIXUP,      // Not real JIT helper. Used for PInvoke target fixup in native images.
    CORINFO_HELP_EE_VSD_FIXUP,          // Not real JIT helper. Used for VSD cell fixup in native images.
    CORINFO_HELP_EE_EXTERNAL_FIXUP,     // Not real JIT helper. Used for to fixup external method thunks in native images.
    CORINFO_HELP_EE_VTABLE_FIXUP,       // Not real JIT helper. Used for inherited vtable slot fixup in native images.

    CORINFO_HELP_EE_REMOTING_THUNK,     // Not real JIT helper. Used for remoting precode in native images.

    CORINFO_HELP_EE_PERSONALITY_ROUTINE,// Not real JIT helper. Used in native images.
    CORINFO_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET,// Not real JIT helper. Used in native images to detect filter funclets.

    //
    // Keep platform-specific helpers at the end so that the ids for the platform neutral helpers stay same accross platforms
    //

#if defined(_TARGET_X86_) || defined(_HOST_X86_) || defined(REDHAWK) // _HOST_X86_ is for altjit
                                    // NOGC_WRITE_BARRIERS JIT helper calls
                                    // Unchecked versions EDX is required to point into GC heap
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
#endif

#if defined(MDIL) && defined(_TARGET_ARM_)
    CORINFO_HELP_ALLOCA,        // this is a "pseudo" helper call for MDIL on ARM; it is NOT implemented in the VM!
                                // Instead the MDIL binder generates "inline" code for it.
#endif // MDIL && _TARGET_ARM_

    CORINFO_HELP_LOOP_CLONE_CHOICE_ADDR, // Return the reference to a counter to decide to take cloned path in debug stress.
    CORINFO_HELP_DEBUG_LOG_LOOP_CLONING, // Print a message that a loop cloning optimization has occurred in debug mode.

#if COR_JIT_EE_VERSION > 460
    CORINFO_HELP_THROW_ARGUMENTEXCEPTION,           // throw ArgumentException
    CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION, // throw ArgumentOutOfRangeException
#endif

    CORINFO_HELP_COUNT,
};

#define CORINFO_HELP_READYTORUN_ATYPICAL_CALLSITE 0x40000000

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
    CORINFO_HELP_SIG_8_VA, //2 arguments plus varargs

    CORINFO_HELP_SIG_EBPCALL, //special calling convention that uses EDX and
                              //EBP as arguments

    CORINFO_HELP_SIG_CANNOT_USE_ALIGN_STUB,

    CORINFO_HELP_SIG_COUNT
};



// MdilHelpFunc defines the set of helpers in a stable fashion; thereby allowing the VM
// to change the ordinals of any helper value without invalidating MDIL images.
// To avoid "accidental" changes of MdilHelpFunc values the enum uses explicit values;
// once a value has been assigned and published, it cannot be easily taken back (only
// in connection with MDIL/CTL/CLR versioning restrictions)
//
// The client side binder will convert the MDIL helper back into the VM specific helper number.
//
// The association between MDIL helpers and "corinfo" helpers is defined in inc\jithelpers.h
// The signatures of the MDIL helpers are defined in inc\MDILHelpers.h (using CorInfoHelpSig)
// TritonToDo: use a more precise/detailed signature mechanism
// Please note that some jit helpers (or groups of related helpers) are represented
// by MDIL instruction(s) instead and therefore don't have a corresponding MDIL helper.

enum MdilHelpFunc
{
    MDIL_HELP_UNDEF = 0x00,         // invalid value. This should never be used

    /* Arithmetic helpers */

    MDIL_HELP_DIV                = 0x01,         // For the ARM 32-bit integer divide uses a helper call :-(
    MDIL_HELP_MOD                = 0x02,
    MDIL_HELP_UDIV               = 0x03,
    MDIL_HELP_UMOD               = 0x04,

    MDIL_HELP_LLSH               = 0x05,
    MDIL_HELP_LRSH               = 0x06,
    MDIL_HELP_LRSZ               = 0x07,
    MDIL_HELP_LMUL               = 0x08,
    MDIL_HELP_LMUL_OVF           = 0x09,
    MDIL_HELP_ULMUL_OVF          = 0x0A,
    MDIL_HELP_LDIV               = 0x0B,
    MDIL_HELP_LMOD               = 0x0C,
    MDIL_HELP_ULDIV              = 0x0D,
    MDIL_HELP_ULMOD              = 0x0E,
    MDIL_HELP_LNG2DBL            = 0x0F,         // Convert a signed int64 to a double
    MDIL_HELP_ULNG2DBL           = 0x10,         // Convert a unsigned int64 to a double
    MDIL_HELP_DBL2INT            = 0x11,
    MDIL_HELP_DBL2INT_OVF        = 0x12,
    MDIL_HELP_DBL2LNG            = 0x13,
    MDIL_HELP_DBL2LNG_OVF        = 0x14,
    MDIL_HELP_DBL2UINT           = 0x15,
    MDIL_HELP_DBL2UINT_OVF       = 0x16,
    MDIL_HELP_DBL2ULNG           = 0x17,
    MDIL_HELP_DBL2ULNG_OVF       = 0x18,
    MDIL_HELP_FLTREM             = 0x19,
    MDIL_HELP_DBLREM             = 0x1A,
    MDIL_HELP_FLTROUND           = 0x1B,
    MDIL_HELP_DBLROUND           = 0x1C,

    /* Allocating a new object. Always use ICorClassInfo::getNewHelper() to decide 
       which is the right helper to use to allocate an object of a given type. */
    MDIL_HELP_NEW_CROSSCONTEXT   = 0x1D,         // cross context new object
    MDIL_HELP_NEWFAST            = 0x1E,
    MDIL_HELP_NEWSFAST           = 0x1F,         // allocator for small, non-finalizer, non-array object
    MDIL_HELP_NEWSFAST_ALIGN8    = 0x20,         // allocator for small, non-finalizer, non-array object, 8 byte aligned
    MDIL_HELP_NEW_MDARR          = 0x21,         // multi-dim array helper (with or without lower bounds)
    MDIL_HELP_STRCNS             = 0x22,         // create a new string literal

    /* Object model */

    MDIL_HELP_INITCLASS          = 0x23,         // Initialize class if not already initialized
    MDIL_HELP_INITINSTCLASS      = 0x24,         // Initialize class for instantiated type

    // Use ICorClassInfo::getCastingHelper to determine
    // the right helper to use

    MDIL_HELP_ISINSTANCEOFINTERFACE = 0x25,      // Optimized helper for interfaces
    MDIL_HELP_ISINSTANCEOFARRAY  = 0x26,         // Optimized helper for arrays
    MDIL_HELP_ISINSTANCEOFCLASS  = 0x27,         // Optimized helper for classes
    MDIL_HELP_CHKCASTINTERFACE   = 0x28,
    MDIL_HELP_CHKCASTARRAY       = 0x29,
    MDIL_HELP_CHKCASTCLASS       = 0x2A,
    MDIL_HELP_CHKCASTCLASS_SPECIAL = 0x2B,       // Optimized helper for classes. Assumes that the trivial cases 
                                                 // has been taken care of by the inlined check
    MDIL_HELP_UNBOX_NULLABLE     = 0x2C,         // special form of unboxing for Nullable<T>
    MDIL_HELP_GETREFANY          = 0x2D,         // Extract the byref from a TypedReference, checking that it is the expected type

    MDIL_HELP_ARRADDR_ST         = 0x2E,         // assign to element of object array with type-checking
    MDIL_HELP_LDELEMA_REF        = 0x2F,         // does a precise type comparision and returns address

    /* Exceptions */
    MDIL_HELP_USER_BREAKPOINT    = 0x30,         // For a user program to break to the debugger
    MDIL_HELP_RNGCHKFAIL         = 0x31,         // array bounds check failed
    MDIL_HELP_OVERFLOW           = 0x32,         // throw an overflow exception

    MDIL_HELP_INTERNALTHROW      = 0x33,         // Support for really fast jit
    MDIL_HELP_VERIFICATION       = 0x34,         // Throw a VerificationException
    MDIL_HELP_SEC_UNMGDCODE_EXCPT= 0x35,         // throw a security unmanaged code exception
    MDIL_HELP_FAIL_FAST          = 0x36,         // Kill the process avoiding any exceptions or stack and data dependencies (use for GuardStack unsafe buffer checks)

    MDIL_HELP_METHOD_ACCESS_EXCEPTION = 0x37,    //Throw an access exception due to a failed member/class access check.
    MDIL_HELP_FIELD_ACCESS_EXCEPTION  = 0x38,
    MDIL_HELP_CLASS_ACCESS_EXCEPTION  = 0x39,

    MDIL_HELP_ENDCATCH           = 0x3A,         // call back into the EE at the end of a catch block

    /* Synchronization */

    MDIL_HELP_MON_ENTER          = 0x3B,
    MDIL_HELP_MON_EXIT           = 0x3C,
    MDIL_HELP_MON_ENTER_STATIC   = 0x3D,
    MDIL_HELP_MON_EXIT_STATIC    = 0x3E,

    MDIL_HELP_GETCLASSFROMMETHODPARAM = 0x3F,    // Given a generics method handle, returns a class handle
    MDIL_HELP_GETSYNCFROMCLASSHANDLE  = 0x40,    // Given a generics class handle, returns the sync monitor 
                                                 // in its ManagedClassObject

    /* Security callout support */
    
    MDIL_HELP_SECURITY_PROLOG    = 0x41,         // Required if CORINFO_FLG_SECURITYCHECK is set, or CORINFO_FLG_NOSECURITYWRAP is not set
    MDIL_HELP_SECURITY_PROLOG_FRAMED = 0x42,     // Slow version of MDIL_HELP_SECURITY_PROLOG. Used for instrumentation.

    MDIL_HELP_METHOD_ACCESS_CHECK    = 0x43,     // Callouts to runtime security access checks
    MDIL_HELP_FIELD_ACCESS_CHECK     = 0x44,
    MDIL_HELP_CLASS_ACCESS_CHECK     = 0x45,

    MDIL_HELP_DELEGATE_SECURITY_CHECK= 0x46,     // Callout to delegate security transparency check

     /* Verification runtime callout support */

    MDIL_HELP_VERIFICATION_RUNTIME_CHECK=0x47,   // Do a Demand for UnmanagedCode permission at runtime

    /* GC support */

    MDIL_HELP_STOP_FOR_GC        = 0x48,         // Call GC (force a GC)
    MDIL_HELP_POLL_GC            = 0x49,         // Ask GC if it wants to collect

    MDIL_HELP_STRESS_GC          = 0x4A,         // Force a GC, but then update the JITTED code to be a noop call
    MDIL_HELP_CHECK_OBJ          = 0x4B,         // confirm that ECX is a valid object pointer (debugging only)

    /* GC Write barrier support */

    MDIL_HELP_ASSIGN_REF         = 0x4C,         // universal helpers with F_CALL_CONV calling convention
    MDIL_HELP_CHECKED_ASSIGN_REF = 0x4D,

    MDIL_HELP_ASSIGN_BYREF       = 0x4E,
    MDIL_HELP_ASSIGN_STRUCT      = 0x4F,


    /* Accessing fields */

    // For COM object support (using COM get/set routines to update object)
    // and EnC and cross-context support
    MDIL_HELP_GETFIELD32         = 0x50,
    MDIL_HELP_SETFIELD32         = 0x51,
    MDIL_HELP_GETFIELD64         = 0x52,
    MDIL_HELP_SETFIELD64         = 0x53,
    MDIL_HELP_GETFIELDOBJ        = 0x54,
    MDIL_HELP_SETFIELDOBJ        = 0x55,
    MDIL_HELP_GETFIELDSTRUCT     = 0x56,
    MDIL_HELP_SETFIELDSTRUCT     = 0x57,
    MDIL_HELP_GETFIELDFLOAT      = 0x58,
    MDIL_HELP_SETFIELDFLOAT      = 0x59,
    MDIL_HELP_GETFIELDDOUBLE     = 0x5A,
    MDIL_HELP_SETFIELDDOUBLE     = 0x5B,

    MDIL_HELP_GETFIELDADDR       = 0x5C,

    MDIL_HELP_GETSTATICFIELDADDR_CONTEXT = 0x5D,    // Helper for context-static fields
    MDIL_HELP_GETSTATICFIELDADDR_TLS     = 0x5E,    // Helper for PE TLS fields

    // There are a variety of specialized helpers for accessing static fields. The JIT should use 
    // ICorClassInfo::getSharedStaticsOrCCtorHelper to determine which helper to use
    
    /* Debugger */

    MDIL_HELP_DBG_IS_JUST_MY_CODE= 0x5F,         // Check if this is "JustMyCode" and needs to be stepped through.

    /* Profiling enter/leave probe addresses */
    MDIL_HELP_PROF_FCN_ENTER     = 0x60,         // record the entry to a method (caller)
    MDIL_HELP_PROF_FCN_LEAVE     = 0x61,         // record the completion of current method (caller)
    MDIL_HELP_PROF_FCN_TAILCALL  = 0x62,         // record the completionof current method through tailcall (caller)

    /* Miscellaneous */

    MDIL_HELP_BBT_FCN_ENTER      = 0x63,         // record the entry to a method for collecting Tuning data

    MDIL_HELP_PINVOKE_CALLI      = 0x64,         // Indirect pinvoke call
    MDIL_HELP_TAILCALL           = 0x65,         // Perform a tail call
    
    MDIL_HELP_GETCURRENTMANAGEDTHREADID = 0x66,

    MDIL_HELP_INIT_PINVOKE_FRAME = 0x67,         // initialize an inlined PInvoke Frame for the JIT-compiler
    MDIL_HELP_CHECK_PINVOKE_DOMAIN = 0x68,       // check which domain the pinvoke call is in

    MDIL_HELP_MEMSET             = 0x69,         // Init block of memory
    MDIL_HELP_MEMCPY             = 0x6A,         // Copy block of memory

    MDIL_HELP_RUNTIMEHANDLE_METHOD               = 0x6B, // determine a type/field/method handle at run-time
    MDIL_HELP_RUNTIMEHANDLE_METHOD_LOG           = 0x6C, // determine a type/field/method handle at run-time, with IBC logging
    MDIL_HELP_RUNTIMEHANDLE_CLASS                = 0x6D, // determine a type/field/method handle at run-time
    MDIL_HELP_RUNTIMEHANDLE_CLASS_LOG            = 0x6E, // determine a type/field/method handle at run-time, with IBC logging
    MDIL_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE    = 0x6F, // Convert from a TypeHandle (native structure pointer) to RuntimeTypeHandle at run-time
    MDIL_HELP_METHODDESC_TO_RUNTIMEMETHODHANDLE  = 0x70, // Convert from a MethodDesc (native structure pointer) to RuntimeMethodHandle at run-time
    MDIL_HELP_FIELDDESC_TO_RUNTIMEFIELDHANDLE    = 0x71, // Convert from a FieldDesc (native structure pointer) to RuntimeFieldHandle at run-time
    MDIL_HELP_TYPEHANDLE_TO_RUNTIMETYPE          = 0x72, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time
    MDIL_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD    = 0x73, // Convert from a MethodDesc (native structure pointer) to RuntimeMethodHandle at run-time
    MDIL_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD      = 0x74, // Convert from a FieldDesc (native structure pointer) to RuntimeFieldHandle at run-time

    MDIL_HELP_VIRTUAL_FUNC_PTR   = 0x75,      // look up a virtual method at run-time

    MDIL_HELP_EE_PRESTUB         = 0x76,         // Not real JIT helper. Used in native images.

    MDIL_HELP_EE_PRECODE_FIXUP   = 0x77,         // Not real JIT helper. Used for Precode fixup in native images.
    MDIL_HELP_EE_PINVOKE_FIXUP   = 0x78,         // Not real JIT helper. Used for PInvoke target fixup in native images.
    MDIL_HELP_EE_VSD_FIXUP       = 0x79,         // Not real JIT helper. Used for VSD cell fixup in native images.
    MDIL_HELP_EE_EXTERNAL_FIXUP  = 0x7A,         // Not real JIT helper. Used for to fixup external method thunks in native images.
    MDIL_HELP_EE_VTABLE_FIXUP    = 0x7B,         // Not real JIT helper. Used for inherited vtable slot fixup in native images.

    MDIL_HELP_EE_REMOTING_THUNK  = 0x7C,         // Not real JIT helper. Used for remoting precode in native images.

    MDIL_HELP_EE_PERSONALITY_ROUTINE=0x7D,       // Not real JIT helper. Used in native images.

    //
    // Keep platform-specific helpers at the end so that the ids for the platform neutral helpers stay same accross platforms
    //

#if defined(_TARGET_X86_) || defined(_HOST_X86_) || defined(REDHAWK) // _HOST_X86_ is for altjit
                                    // NOGC_WRITE_BARRIERS JIT helper calls
                                    // Unchecked versions EDX is required to point into GC heap
    MDIL_HELP_ASSIGN_REF_EAX     = 0x7E,         // EAX holds GC ptr, do a 'mov [EDX], EAX' and inform GC
    MDIL_HELP_ASSIGN_REF_EBX     = 0x7F,         // EBX holds GC ptr, do a 'mov [EDX], EBX' and inform GC
    MDIL_HELP_ASSIGN_REF_ECX     = 0x80,         // ECX holds GC ptr, do a 'mov [EDX], ECX' and inform GC
    MDIL_HELP_ASSIGN_REF_ESI     = 0x81,         // ESI holds GC ptr, do a 'mov [EDX], ESI' and inform GC
    MDIL_HELP_ASSIGN_REF_EDI     = 0x82,         // EDI holds GC ptr, do a 'mov [EDX], EDI' and inform GC
    MDIL_HELP_ASSIGN_REF_EBP     = 0x83,         // EBP holds GC ptr, do a 'mov [EDX], EBP' and inform GC

    MDIL_HELP_CHECKED_ASSIGN_REF_EAX = 0x84,     // These are the same as ASSIGN_REF above ...
    MDIL_HELP_CHECKED_ASSIGN_REF_EBX = 0x85,     // ... but also check if EDX points into heap.
    MDIL_HELP_CHECKED_ASSIGN_REF_ECX = 0x86,
    MDIL_HELP_CHECKED_ASSIGN_REF_ESI = 0x87,
    MDIL_HELP_CHECKED_ASSIGN_REF_EDI = 0x88,
    MDIL_HELP_CHECKED_ASSIGN_REF_EBP = 0x89,
#endif

    MDIL_HELP_ASSIGN_REF_ENSURE_NONHEAP = 0x8A,  // Do the store, and ensure that the target was not in the heap.

#if !defined(_TARGET_X86_)
    MDIL_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET = 0x90,
#endif 

#if defined(_TARGET_ARM_)
    MDIL_HELP_ALLOCA             = 0x9A,         // this is a "pseudo" helper call for MDIL on ARM; it is NOT implemented in the VM!
                                                 // Instead the MDIL binder generates "inline" code for it.
#endif // _TARGET_ARM_

    MDIL_HELP_GETFIELD8          = 0xA0,
    MDIL_HELP_SETFIELD8          = 0xA1,
    MDIL_HELP_GETFIELD16         = 0xA2,
    MDIL_HELP_SETFIELD16         = 0xA3,

#ifdef REDHAWK
    // these helpers are arbitrary since we don't have any relation to the actual CLR corinfo.h.
    MDIL_HELP_PINVOKE            = 0xB0,         // transition to preemptive mode for a pinvoke, frame in EAX
    MDIL_HELP_PINVOKE_2          = 0xB1,         // transition to preemptive mode for a pinvoke, frame in ESI / R10
    MDIL_HELP_PINVOKE_RETURN     = 0xB2,         // return to cooperative mode from a pinvoke
    MDIL_HELP_REVERSE_PINVOKE    = 0xB3,         // transition to cooperative mode for a callback from native
    MDIL_HELP_REVERSE_PINVOKE_RETURN = 0xB4,     // return to preemptive mode to return to native from managed
    MDIL_HELP_REGISTER_MODULE    = 0xB5,         // module load notification
    MDIL_HELP_CREATECOMMANDLINE  = 0xB6,         // get the command line from the system and return it for Main
    MDIL_HELP_VSD_INITIAL_TARGET = 0xB7,         // all VSD indirection cells initially point to this function
    MDIL_HELP_NEW_FINALIZABLE    = 0xB8,         // allocate finalizable object
    MDIL_HELP_SHUTDOWN           = 0xB9,         // called when Main returns from a managed executable
    MDIL_HELP_CHECKARRAYSTORE    = 0xBA,         // checks that an array element assignment is of the right type
    MDIL_HELP_CHECK_VECTOR_ELEM_ADDR = 0xBB,     // does a precise type check on the array element type
    MDIL_HELP_FLT2INT_OVF        = 0xBC,         // checked float->int conversion
    MDIL_HELP_FLT2LNG            = 0xBD,         // float->long conversion
    MDIL_HELP_FLT2LNG_OVF        = 0xBE,         // checked float->long conversion
    MDIL_HELP_FLTREM_REV         = 0xBF,         // Bartok helper for float remainder - uses reversed param order from CLR helper
    MDIL_HELP_DBLREM_REV         = 0xC0,         // Bartok helper for double remainder - uses reversed param order from CLR helper
    MDIL_HELP_HIJACKFORGCSTRESS  = 0xC1,         // this helper hijacks the caller for GC stress
    MDIL_HELP_INIT_GCSTRESS      = 0xC2,         // this helper initializes the runtime for GC stress
    MDIL_HELP_SUPPRESS_GCSTRESS  = 0xC3,         // disables gc stress
    MDIL_HELP_UNSUPPRESS_GCSTRESS= 0xC4,         // re-enables gc stress
    MDIL_HELP_THROW_INTRA        = 0xC5,         // Throw an exception object to a hander within the method
    MDIL_HELP_THROW_INTER        = 0xC6,         // Throw an exception object to a hander within the caller
    MDIL_HELP_THROW_ARITHMETIC   = 0xC7,         // Throw the classlib-defined arithmetic exception
    MDIL_HELP_THROW_DIVIDE_BY_ZERO = 0xC8,       // Throw the classlib-defined divide by zero exception
    MDIL_HELP_THROW_INDEX        = 0xC9,         // Throw the classlib-defined index out of range exception
    MDIL_HELP_THROW_OVERFLOW     = 0xCA,         // Throw the classlib-defined overflow exception
    MDIL_HELP_EHJUMP_SCALAR      = 0xCB,         // Helper to jump to a handler in a different method for EH dispatch.
    MDIL_HELP_EHJUMP_OBJECT      = 0xCC,         // Helper to jump to a handler in a different method for EH dispatch.
    MDIL_HELP_EHJUMP_BYREF       = 0xCD,         // Helper to jump to a handler in a different method for EH dispatch.
    MDIL_HELP_EHJUMP_SCALAR_GCSTRESS = 0xCE,     // Helper to jump to a handler in a different method for EH dispatch.
    MDIL_HELP_EHJUMP_OBJECT_GCSTRESS = 0XCF,     // Helper to jump to a handler in a different method for EH dispatch.
    MDIL_HELP_EHJUMP_BYREF_GCSTRESS  = 0xD0,     // Helper to jump to a handler in a different method for EH dispatch.

    // Bartok emits code with destination in ECX rather than EDX and only ever uses EDX as the reference
    // register. It also only ever specifies the checked version.
    MDIL_HELP_CHECKED_ASSIGN_REF_EDX = 0xD1, // EDX hold GC ptr, want do a 'mov [ECX], EDX' and inform GC
    MDIL_HELP_COUNT              = MDIL_HELP_CHECKED_ASSIGN_REF_EDX+1,
#else
    MDIL_HELP_COUNT              = 0xA4,
#endif // REDHAWK



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
    CORINFO_TYPE_MOD_PINNED      = 0x40,        // can be applied to CLASS, or BYREF to indiate pinned
};

inline CorInfoType strip(CorInfoTypeWithMod val) {
    return CorInfoType(val & CORINFO_TYPE_MASK);
}

// The enumeration is returned in 'getSig'

enum CorInfoCallConv
{
    // These correspond to CorCallingConvention

    CORINFO_CALLCONV_DEFAULT    = 0x0,
    CORINFO_CALLCONV_C          = 0x1,
    CORINFO_CALLCONV_STDCALL    = 0x2,
    CORINFO_CALLCONV_THISCALL   = 0x3,
    CORINFO_CALLCONV_FASTCALL   = 0x4,
    CORINFO_CALLCONV_VARARG     = 0x5,
    CORINFO_CALLCONV_FIELD      = 0x6,
    CORINFO_CALLCONV_LOCAL_SIG  = 0x7,
    CORINFO_CALLCONV_PROPERTY   = 0x8,
    CORINFO_CALLCONV_NATIVEVARARG = 0xb,    // used ONLY for IL stub PInvoke vararg calls

    CORINFO_CALLCONV_MASK       = 0x0f,     // Calling convention is bottom 4 bits
    CORINFO_CALLCONV_GENERIC    = 0x10,
    CORINFO_CALLCONV_HASTHIS    = 0x20,
    CORINFO_CALLCONV_EXPLICITTHIS=0x40,
    CORINFO_CALLCONV_PARAMTYPE  = 0x80,     // Passed last. Same as CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG
};

enum CorInfoUnmanagedCallConv
{
    // These correspond to CorUnmanagedCallingConvention

    CORINFO_UNMANAGED_CALLCONV_UNKNOWN,
    CORINFO_UNMANAGED_CALLCONV_C,
    CORINFO_UNMANAGED_CALLCONV_STDCALL,
    CORINFO_UNMANAGED_CALLCONV_THISCALL,
    CORINFO_UNMANAGED_CALLCONV_FASTCALL
};

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
    CORINFO_FLG_PROTECTED             = 0x00000004,
    CORINFO_FLG_STATIC                = 0x00000008,
    CORINFO_FLG_FINAL                 = 0x00000010,
    CORINFO_FLG_SYNCH                 = 0x00000020,
    CORINFO_FLG_VIRTUAL               = 0x00000040,
//  CORINFO_FLG_UNUSED                = 0x00000080,
    CORINFO_FLG_NATIVE                = 0x00000100,
//  CORINFO_FLG_UNUSED                = 0x00000200,
    CORINFO_FLG_ABSTRACT              = 0x00000400,

    CORINFO_FLG_EnC                   = 0x00000800, // member was added by Edit'n'Continue

    // These are internal flags that can only be on methods
    CORINFO_FLG_FORCEINLINE           = 0x00010000, // The method should be inlined if possible.
    CORINFO_FLG_SHAREDINST            = 0x00020000, // the code for this method is shared between different generic instantiations (also set on classes/types)
    CORINFO_FLG_DELEGATE_INVOKE       = 0x00040000, // "Delegate
    CORINFO_FLG_PINVOKE               = 0x00080000, // Is a P/Invoke call
    CORINFO_FLG_SECURITYCHECK         = 0x00100000, // Is one of the security routines that does a stackwalk (e.g. Assert, Demand)
    CORINFO_FLG_NOGCCHECK             = 0x00200000, // This method is FCALL that has no GC check.  Don't put alone in loops
    CORINFO_FLG_INTRINSIC             = 0x00400000, // This method MAY have an intrinsic ID
    CORINFO_FLG_CONSTRUCTOR           = 0x00800000, // This method is an instance or type initializer
//  CORINFO_FLG_UNUSED                = 0x01000000,
//  CORINFO_FLG_UNUSED                = 0x02000000,
    CORINFO_FLG_NOSECURITYWRAP        = 0x04000000, // The method requires no security checks
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
    CORINFO_FLG_CONTEXTFUL            = 0x00400000, // is this a contextful class?
    CORINFO_FLG_CUSTOMLAYOUT          = 0x00800000, // does this struct have custom layout?
    CORINFO_FLG_CONTAINS_GC_PTR       = 0x01000000, // does the class contain a gc ptr ?
    CORINFO_FLG_DELEGATE              = 0x02000000, // is this a subclass of delegate or multicast delegate ?
    CORINFO_FLG_MARSHAL_BYREF         = 0x04000000, // is this a subclass of MarshalByRef ?
    CORINFO_FLG_CONTAINS_STACK_PTR    = 0x08000000, // This class has a stack pointer inside it
    CORINFO_FLG_VARIANCE              = 0x10000000, // MethodTable::HasVariance (sealed does *not* mean uncast-able)
    CORINFO_FLG_BEFOREFIELDINIT       = 0x20000000, // Additional flexibility for when to run .cctor (see code:#ClassConstructionFlags)
    CORINFO_FLG_GENERIC_TYPE_VARIABLE = 0x40000000, // This is really a handle for a variable type
    CORINFO_FLG_UNSAFE_VALUECLASS     = 0x80000000, // Unsafe (C++'s /GS) value type
};

// Flags computed by a runtime compiler
enum CorInfoMethodRuntimeFlags
{
    CORINFO_FLG_BAD_INLINEE         = 0x00000001, // The method is not suitable for inlining
    CORINFO_FLG_VERIFIABLE          = 0x00000002, // The method has verifiable code
    CORINFO_FLG_UNVERIFIABLE        = 0x00000004, // The method has unverifiable code
};


enum CORINFO_ACCESS_FLAGS
{
    CORINFO_ACCESS_ANY        = 0x0000, // Normal access
    CORINFO_ACCESS_THIS       = 0x0001, // Accessed via the this reference
    CORINFO_ACCESS_UNWRAP     = 0x0002, // Accessed via an unwrap reference

    CORINFO_ACCESS_NONNULL    = 0x0004, // Instance is guaranteed non-null

    CORINFO_ACCESS_LDFTN      = 0x0010, // Accessed via ldftn

    // Field access flags
    CORINFO_ACCESS_GET        = 0x0100, // Field get (ldfld)
    CORINFO_ACCESS_SET        = 0x0200, // Field set (stfld)
    CORINFO_ACCESS_ADDRESS    = 0x0400, // Field address (ldflda)
    CORINFO_ACCESS_INIT_ARRAY = 0x0800, // Field use for InitializeArray
    CORINFO_ACCESS_ATYPICAL_CALLSITE = 0x4000, // Atypical callsite that cannot be disassembled by delay loading helper
    CORINFO_ACCESS_INLINECHECK= 0x8000, // Return fieldFlags and fieldAccessor only. Used by JIT64 during inlining.
};

// These are the flags set on an CORINFO_EH_CLAUSE
enum CORINFO_EH_CLAUSE_FLAGS
{
    CORINFO_EH_CLAUSE_NONE    = 0,
    CORINFO_EH_CLAUSE_FILTER  = 0x0001, // If this bit is on, then this EH entry is for a filter
    CORINFO_EH_CLAUSE_FINALLY = 0x0002, // This clause is a finally clause
    CORINFO_EH_CLAUSE_FAULT   = 0x0004, // This clause is a fault   clause
#ifdef REDHAWK
    CORINFO_EH_CLAUSE_METHOD_BOUNDARY   = 0x0008,       // This clause indicates the boundary of an inlined method
    CORINFO_EH_CLAUSE_FAIL_FAST         = 0x0010,       // This clause will cause the exception to go unhandled
    CORINFO_EH_CLAUSE_INDIRECT_TYPE_REFERENCE = 0x0020, // This clause is typed, but type reference is indirect.
#endif
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


// This enumeration is returned by getIntrinsicID. Methods corresponding to
// these values will have "well-known" specified behavior. Calls to these
// methods could be replaced with inlined code corresponding to the
// specified behavior (without having to examine the IL beforehand).

enum CorInfoIntrinsics
{
    CORINFO_INTRINSIC_Sin,
    CORINFO_INTRINSIC_Cos,
    CORINFO_INTRINSIC_Sqrt,
    CORINFO_INTRINSIC_Abs,
    CORINFO_INTRINSIC_Round,
    CORINFO_INTRINSIC_Cosh,
    CORINFO_INTRINSIC_Sinh,
    CORINFO_INTRINSIC_Tan,
    CORINFO_INTRINSIC_Tanh,
    CORINFO_INTRINSIC_Asin,
    CORINFO_INTRINSIC_Acos,
    CORINFO_INTRINSIC_Atan,
    CORINFO_INTRINSIC_Atan2,
    CORINFO_INTRINSIC_Log10,
    CORINFO_INTRINSIC_Pow,
    CORINFO_INTRINSIC_Exp,
    CORINFO_INTRINSIC_Ceiling,
    CORINFO_INTRINSIC_Floor,
    CORINFO_INTRINSIC_GetChar,              // fetch character out of string
    CORINFO_INTRINSIC_Array_GetDimLength,   // Get number of elements in a given dimension of an array
    CORINFO_INTRINSIC_Array_Get,            // Get the value of an element in an array
    CORINFO_INTRINSIC_Array_Address,        // Get the address of an element in an array
    CORINFO_INTRINSIC_Array_Set,            // Set the value of an element in an array
    CORINFO_INTRINSIC_StringGetChar,        // fetch character out of string
    CORINFO_INTRINSIC_StringLength,         // get the length
    CORINFO_INTRINSIC_InitializeArray,      // initialize an array from static data
    CORINFO_INTRINSIC_GetTypeFromHandle,
    CORINFO_INTRINSIC_RTH_GetValueInternal,
    CORINFO_INTRINSIC_TypeEQ,
    CORINFO_INTRINSIC_TypeNEQ,
    CORINFO_INTRINSIC_Object_GetType,
    CORINFO_INTRINSIC_StubHelpers_GetStubContext,
#ifdef _WIN64
    CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr,
#endif // _WIN64
    CORINFO_INTRINSIC_StubHelpers_GetNDirectTarget,
    CORINFO_INTRINSIC_InterlockedAdd32,
    CORINFO_INTRINSIC_InterlockedAdd64,
    CORINFO_INTRINSIC_InterlockedXAdd32,
    CORINFO_INTRINSIC_InterlockedXAdd64,
    CORINFO_INTRINSIC_InterlockedXchg32,
    CORINFO_INTRINSIC_InterlockedXchg64,
    CORINFO_INTRINSIC_InterlockedCmpXchg32,
    CORINFO_INTRINSIC_InterlockedCmpXchg64,
    CORINFO_INTRINSIC_MemoryBarrier,
    CORINFO_INTRINSIC_GetCurrentManagedThread,
    CORINFO_INTRINSIC_GetManagedThreadId,

    CORINFO_INTRINSIC_Count,
    CORINFO_INTRINSIC_Illegal = -1,         // Not a true intrinsic,
};

// Can a value be accessed directly from JITed code.
enum InfoAccessType
{
    IAT_VALUE,      // The info value is directly available
    IAT_PVALUE,     // The value needs to be accessed via an       indirection
    IAT_PPVALUE     // The value needs to be accessed via a double indirection
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
    INLINE_PASS                 = 0,    // Inlining OK

    // failures are negative
    INLINE_FAIL                 = -1,   // Inlining not OK for this case only
    INLINE_NEVER                = -2,   // This method should never be inlined, regardless of context
};

enum CorInfoInlineRestrictions
{
    INLINE_RESPECT_BOUNDARY = 0x00000001, // You can inline if there are no calls from the method being inlined
    INLINE_NO_CALLEE_LDSTR  = 0x00000002, // You can inline only if you guarantee that if inlinee does an ldstr
                                          // inlinee's module will never see that string (by any means).
                                          // This is due to how we implement the NoStringInterningAttribute
                                          // (by reusing the fixup table).
    INLINE_SAME_THIS        = 0x00000004, // You can inline only if the callee is on the same this reference as caller
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

enum CorInfoCanSkipVerificationResult
{
    CORINFO_VERIFICATION_CANNOT_SKIP    = 0,    // Cannot skip verification during jit time.
    CORINFO_VERIFICATION_CAN_SKIP       = 1,    // Can skip verification during jit time.
    CORINFO_VERIFICATION_RUNTIME_CHECK  = 2,    // Cannot skip verification during jit time,
                                                //     but need to insert a callout to the VM to ask during runtime 
                                                //     whether to raise a verification or not (if the method is unverifiable).
    CORINFO_VERIFICATION_DONT_JIT       = 3,    // Cannot skip verification during jit time,
                                                //     but do not jit the method if is is unverifiable.
};

enum CorInfoInitClassResult
{
    CORINFO_INITCLASS_NOT_REQUIRED  = 0x00, // No class initialization required, but the class is not actually initialized yet 
                                            // (e.g. we are guaranteed to run the static constructor in method prolog)
    CORINFO_INITCLASS_INITIALIZED   = 0x01, // Class initialized
    CORINFO_INITCLASS_SPECULATIVE   = 0x02, // Class may be initialized speculatively
    CORINFO_INITCLASS_USE_HELPER    = 0x04, // The JIT must insert class initialization helper call.
    CORINFO_INITCLASS_DONT_INLINE   = 0x08, // The JIT should not inline the method requesting the class initialization. The class 
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

// This is for use when the JIT is compiling an instantiation
// of generic code.  The JIT needs to know if the generic code itself
// (which can be verified once and for all independently of the
// instantiations) passed verification.
enum CorInfoInstantiationVerification
{
    // The method is NOT a concrete instantiation (eg. List<int>.Add()) of a method 
    // in a generic class or a generic method. It is either the typical instantiation 
    // (eg. List<T>.Add()) or entirely non-generic.
    INSTVER_NOT_INSTANTIATION           = 0,

    // The method is an instantiation of a method in a generic class or a generic method, 
    // and the generic class was successfully verified
    INSTVER_GENERIC_PASSED_VERIFICATION = 1,

    // The method is an instantiation of a method in a generic class or a generic method, 
    // and the generic class failed verification
    INSTVER_GENERIC_FAILED_VERIFICATION = 2,
};

// When using CORINFO_HELPER_TAILCALL, the JIT needs to pass certain special
// calling convention/argument passing/handling details to the helper
enum CorInfoHelperTailCallSpecialHandling
{
    CORINFO_TAILCALL_NORMAL =               0x00000000,
    CORINFO_TAILCALL_STUB_DISPATCH_ARG =    0x00000001,
};


inline bool dontInline(CorInfoInline val) {
    return(val < 0);
}

// Cookie types consumed by the code generator (these are opaque values
// not inspected by the code generator):

typedef struct CORINFO_ASSEMBLY_STRUCT_*    CORINFO_ASSEMBLY_HANDLE;
typedef struct CORINFO_MODULE_STRUCT_*      CORINFO_MODULE_HANDLE;
typedef struct CORINFO_DEPENDENCY_STRUCT_*  CORINFO_DEPENDENCY_HANDLE;
typedef struct CORINFO_CLASS_STRUCT_*       CORINFO_CLASS_HANDLE;
typedef struct CORINFO_METHOD_STRUCT_*      CORINFO_METHOD_HANDLE;
typedef struct CORINFO_FIELD_STRUCT_*       CORINFO_FIELD_HANDLE;
typedef struct CORINFO_ARG_LIST_STRUCT_*    CORINFO_ARG_LIST_HANDLE;    // represents a list of argument types
typedef struct CORINFO_JUST_MY_CODE_HANDLE_*CORINFO_JUST_MY_CODE_HANDLE;
typedef struct CORINFO_PROFILING_STRUCT_*   CORINFO_PROFILING_HANDLE;   // a handle guaranteed to be unique per process
typedef struct CORINFO_GENERIC_STRUCT_*     CORINFO_GENERIC_HANDLE;     // a generic handle (could be any of the above)

// what is actually passed on the varargs call
typedef struct CORINFO_VarArgInfo *         CORINFO_VARARGS_HANDLE;

// Generic tokens are resolved with respect to a context, which is usually the method
// being compiled. The CORINFO_CONTEXT_HANDLE indicates which exact instantiation
// (or the open instantiation) is being referred to.
// CORINFO_CONTEXT_HANDLE is more tightly scoped than CORINFO_MODULE_HANDLE. For cases 
// where the exact instantiation does not matter, CORINFO_MODULE_HANDLE is used.
typedef CORINFO_METHOD_HANDLE               CORINFO_CONTEXT_HANDLE;

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

#define MAKE_CLASSCONTEXT(c)  (CORINFO_CONTEXT_HANDLE((size_t) (c) | CORINFO_CONTEXTFLAGS_CLASS))
#define MAKE_METHODCONTEXT(m) (CORINFO_CONTEXT_HANDLE((size_t) (m) | CORINFO_CONTEXTFLAGS_METHOD))

enum CorInfoSigInfoFlags
{
    CORINFO_SIGFLAG_IS_LOCAL_SIG = 0x01,
    CORINFO_SIGFLAG_IL_STUB      = 0x02,
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
    struct CORINFO_SIG_INST sigInst;  // information about how type variables are being instantiated in generic code
    CORINFO_ARG_LIST_HANDLE args;
    PCCOR_SIGNATURE         pSig;
    unsigned                cbSig;
    CORINFO_MODULE_HANDLE   scope;          // passed to getArgClass
    mdToken                 token;

    CorInfoCallConv     getCallConv()       { return CorInfoCallConv((callConv & CORINFO_CALLCONV_MASK)); }
    bool                hasThis()           { return ((callConv & CORINFO_CALLCONV_HASTHIS) != 0); }
    bool                hasExplicitThis()   { return ((callConv & CORINFO_CALLCONV_EXPLICITTHIS) != 0); }
    unsigned            totalILArgs()       { return (numArgs + hasThis()); }
    bool                isVarArg()          { return ((getCallConv() == CORINFO_CALLCONV_VARARG) || (getCallConv() == CORINFO_CALLCONV_NATIVEVARARG)); }
    bool                hasTypeArg()        { return ((callConv & CORINFO_CALLCONV_PARAMTYPE) != 0); }
};

#ifdef  MDIL
struct  CORINFO_EH_CLAUSE;
struct  InlineContext;
#endif

struct CORINFO_METHOD_INFO
{
    CORINFO_METHOD_HANDLE       ftn;
    CORINFO_MODULE_HANDLE       scope;
    BYTE *                      ILCode;
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
    //     IAT_VALUE   --> "handle" stores the real handle or "addr " stores the computed address
    //     IAT_PVALUE  --> "addr" stores a pointer to a location which will hold the real handle
    //     IAT_PPVALUE --> "addr" stores a double indirection to a location which will hold the real handle

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
};

struct CORINFO_LOOKUP_KIND
{
    bool                        needsRuntimeLookup;
    CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind;
} ;

// CORINFO_RUNTIME_LOOKUP indicates the details of the runtime lookup
// operation to be performed.
//
// CORINFO_MAXINDIRECTIONS is the maximum number of
// indirections used by runtime lookups.
// This accounts for up to 2 indirections to get at a dictionary followed by a possible spill slot
//
#define CORINFO_MAXINDIRECTIONS 4
#define CORINFO_USEHELPER ((WORD) 0xffff)

struct CORINFO_RUNTIME_LOOKUP
{
    // This is signature you must pass back to the runtime lookup helper
    LPVOID                  signature;

    // Here is the helper you must call. It is one of CORINFO_HELP_RUNTIMEHANDLE_* helpers.
    CorInfoHelpFunc         helper;

    // Number of indirections to get there
    // CORINFO_USEHELPER = don't know how to get it, so use helper function at run-time instead
    // 0 = use the this pointer itself (e.g. token is C<!0> inside code in sealed class C)
    //     or method desc itself (e.g. token is method void M::mymeth<!!0>() inside code in M::mymeth)
    // Otherwise, follow each byte-offset stored in the "offsets[]" array (may be negative)
    WORD                    indirections;

    // If set, test for null and branch to helper if null
    bool                    testForNull;

    // If set, test the lowest bit and dereference if set (see code:FixupPointer)
    bool                    testForFixup;

    SIZE_T                  offsets[CORINFO_MAXINDIRECTIONS];
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
#ifdef  MDIL
    DWORD token;
#endif
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
// For evertyhing besides "constrained." calls "thisTransform" is set to
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
    CORINFO_CALLINFO_KINDONLY       = 0x0004,   // This is set to only query the kind of call to perform, without getting any other information
    CORINFO_CALLINFO_VERIFICATION   = 0x0008,   // Gets extra verification information.
    CORINFO_CALLINFO_SECURITYCHECKS = 0x0010,   // Perform security checks.
    CORINFO_CALLINFO_LDFTN          = 0x0020,   // Resolving target of LDFTN
    CORINFO_CALLINFO_ATYPICAL_CALLSITE = 0x0040, // Atypical callsite that cannot be disassembled by delay loading helper
};

enum CorInfoIsAccessAllowedResult
{
    CORINFO_ACCESS_ALLOWED = 0,           // Call allowed
    CORINFO_ACCESS_ILLEGAL = 1,           // Call not allowed
    CORINFO_ACCESS_RUNTIME_CHECK = 2,     // Ask at runtime whether to allow the call or not
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
    ULONG                   cbTypeSpec;
    PCCOR_SIGNATURE         pMethodSpec;
    ULONG                   cbMethodSpec;
};

struct CORINFO_CALL_INFO
{
    CORINFO_METHOD_HANDLE   hMethod;            //target method handle
    unsigned                methodFlags;        //flags for the target method

    unsigned                classFlags;         //flags for CORINFO_RESOLVED_TOKEN::hClass

    CORINFO_SIG_INFO       sig;

    //Verification information
    unsigned                verMethodFlags;     // flags for CORINFO_RESOLVED_TOKEN::hMethod
    CORINFO_SIG_INFO        verSig;
    //All of the regular method data is the same... hMethod might not be the same as CORINFO_RESOLVED_TOKEN::hMethod


    //If set to:
    //  - CORINFO_ACCESS_ALLOWED - The access is allowed.
    //  - CORINFO_ACCESS_ILLEGAL - This access cannot be allowed (i.e. it is public calling private).  The
    //      JIT may either insert the callsiteCalloutHelper into the code (as per a verification error) or
    //      call throwExceptionFromHelper on the callsiteCalloutHelper.  In this case callsiteCalloutHelper
    //      is guaranteed not to return.
    //  - CORINFO_ACCESS_RUNTIME_CHECK - The jit must insert the callsiteCalloutHelper at the call site.
    //      the helper may return
    CorInfoIsAccessAllowedResult accessAllowed;
    CORINFO_HELPER_DESC     callsiteCalloutHelper;

    // See above section on constraintCalls to understand when these are set to unusual values.
    CORINFO_THIS_TRANSFORM  thisTransform;

    CORINFO_CALL_KIND       kind;
    BOOL                    nullInstanceCheck;

    // Context for inlining and hidden arg
    CORINFO_CONTEXT_HANDLE  contextHandle;
    BOOL                    exactContextNeedsRuntimeLookup; // Set if contextHandle is approx handle. Runtime lookup is required to get the exact handle.

    // If kind.CORINFO_VIRTUALCALL_STUB then stubLookup will be set.
    // If kind.CORINFO_CALL_CODE_POINTER then entryPointLookup will be set.
    union
    {
        CORINFO_LOOKUP      stubLookup;

        CORINFO_LOOKUP      codePointerLookup;
    };

    CORINFO_CONST_LOOKUP    instParamLookup;    // Used by Ready-to-Run
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

    CORINFO_FIELD_INTRINSIC_ZERO,           // intrinsic zero (IntPtr.Zero, UIntPtr.Zero)
    CORINFO_FIELD_INTRINSIC_EMPTY_STRING,   // intrinsic emptry string (String.Empty)
};

// Set of flags returned in CORINFO_FIELD_INFO::fieldFlags
enum CORINFO_FIELD_FLAGS
{
    CORINFO_FLG_FIELD_STATIC                    = 0x00000001,
    CORINFO_FLG_FIELD_UNMANAGED                 = 0x00000002, // RVA field
    CORINFO_FLG_FIELD_FINAL                     = 0x00000004,
    CORINFO_FLG_FIELD_STATIC_IN_HEAP            = 0x00000008, // See code:#StaticFields. This static field is in the GC heap as a boxed object
    CORINFO_FLG_FIELD_SAFESTATIC_BYREF_RETURN   = 0x00000010, // Field can be returned safely (has GC heap lifetime)
    CORINFO_FLG_FIELD_INITCLASS                 = 0x00000020, // initClass has to be called before accessing the field
    CORINFO_FLG_FIELD_PROTECTED                 = 0x00000040,
};

struct CORINFO_FIELD_INFO
{
    CORINFO_FIELD_ACCESSOR  fieldAccessor;
    unsigned                fieldFlags;

    // Helper to use if the field access requires it
    CorInfoHelpFunc         helper;

    // Field offset if there is one
    DWORD                   offset;

    CorInfoType             fieldType;
    CORINFO_CLASS_HANDLE    structType; //possibly null

    //See CORINFO_CALL_INFO.accessAllowed
    CorInfoIsAccessAllowedResult accessAllowed;
    CORINFO_HELPER_DESC     accessCalloutHelper;

    CORINFO_CONST_LOOKUP    fieldLookup;        // Used by Ready-to-Run
};

//----------------------------------------------------------------------------
// Exception handling

struct CORINFO_EH_CLAUSE
{
    CORINFO_EH_CLAUSE_FLAGS     Flags;
    DWORD                       TryOffset;
    DWORD                       TryLength;
    DWORD                       HandlerOffset;
    DWORD                       HandlerLength;
    union
    {
        DWORD                   ClassToken;       // use for type-based exception handlers
        DWORD                   FilterOffset;     // use for filter-based exception handlers (COR_ILEXCEPTION_FILTER is set)
#ifdef REDHAWK
        void *                  EETypeReference;  // use to hold a ref to the EEType for type-based exception handlers.
#endif
    };
};

enum CORINFO_OS
{
    CORINFO_WINNT,
    CORINFO_PAL,
};

struct CORINFO_CPU
{
    DWORD           dwCPUType;
    DWORD           dwFeatures;
    DWORD           dwExtendedFeatures;
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
    }
    inlinedCallFrameInfo;
   
    // Offsets into the Thread structure
    unsigned    offsetOfThreadFrame;            // offset of the current Frame
    unsigned    offsetOfGCState;                // offset of the preemptive/cooperative state of the Thread

    // Delegate offsets
    unsigned    offsetOfDelegateInstance;
    unsigned    offsetOfDelegateFirstTarget;

    // Remoting offsets
    unsigned    offsetOfTransparentProxyRP;
    unsigned    offsetOfRealProxyServer;

    // Array offsets
    unsigned    offsetOfObjArrayData;

    CORINFO_OS  osType;
    unsigned    osMajor;
    unsigned    osMinor;
    unsigned    osBuild;
};

// This is used to indicate that a finally has been called 
// "locally" by the try block
enum { LCL_FINALLY_MARK = 0xFC }; // FC = "Finally Call"

/**********************************************************************************
 * The following is the internal structure of an object that the compiler knows about
 * when it generates code
 **********************************************************************************/
#include <pshpack4.h>

#define CORINFO_PAGE_SIZE   0x1000                           // the page size on the machine

// <TODO>@TODO: put this in the CORINFO_EE_INFO data structure</TODO>

#ifndef FEATURE_PAL
#define MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT ((32*1024)-1)   // when generating JIT code
#else // !FEATURE_PAL
#define MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT ((OS_PAGE_SIZE / 2) - 1)
#endif // !FEATURE_PAL

typedef void* CORINFO_MethodPtr;            // a generic method pointer

struct CORINFO_Object
{
    CORINFO_MethodPtr      *methTable;      // the vtable for the object
};

struct CORINFO_String : public CORINFO_Object
{
    unsigned                stringLen;
    const wchar_t           chars[1];       // actually of variable size
};

struct CORINFO_Array : public CORINFO_Object
{
    unsigned                length;
#ifdef _WIN64
    unsigned                alignpad;
#endif // _WIN64

#if 0
    /* Multi-dimensional arrays have the lengths and bounds here */
    unsigned                dimLength[length];
    unsigned                dimBound[length];
#endif

    union
    {
        __int8              i1Elems[1];    // actually of variable size
        unsigned __int8     u1Elems[1];
        __int16             i2Elems[1];
        unsigned __int16    u2Elems[1];
        __int32             i4Elems[1];
        unsigned __int32    u4Elems[1];
        float               r4Elems[1];
    };
};

#include <pshpack4.h>
struct CORINFO_Array8 : public CORINFO_Object
{
    unsigned                length;
#ifdef _WIN64
    unsigned                alignpad;
#endif // _WIN64

    union
    {
        double              r8Elems[1];
        __int64             i8Elems[1];
        unsigned __int64    u8Elems[1];
    };
};

#include <poppack.h>

struct CORINFO_RefArray : public CORINFO_Object
{
    unsigned                length;
#ifdef _WIN64
    unsigned                alignpad;
#endif // _WIN64

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

#include <poppack.h>

enum CorInfoSecurityRuntimeChecks
{
    CORINFO_ACCESS_SECURITY_NONE                          = 0,
    CORINFO_ACCESS_SECURITY_TRANSPARENCY                  = 0x0001  // check that transparency rules are enforced between the caller and callee
};


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
#ifndef offsetof
#define offsetof(s,m)   ((size_t)&(((s *)0)->m))
#endif

// Guard-stack cookie for preventing against stack buffer overruns
typedef SIZE_T GSCookie;

/**********************************************************************************/
// DebugInfo types shared by JIT-EE interface and EE-Debugger interface

class ICorDebugInfo
{
public:
    /*----------------------------- Boundary-info ---------------------------*/

    enum MappingTypes
    {
        NO_MAPPING  = -1,
        PROLOG      = -2,
        EPILOG      = -3,
        MAX_MAPPING_VALUE = -3 // Sentinal value. This should be set to the largest magnitude value in the enum
                               // so that the compression routines know the enum's range.
    };

    enum BoundaryTypes
    {
        NO_BOUNDARIES           = 0x00,     // No implicit boundaries
        STACK_EMPTY_BOUNDARIES  = 0x01,     // Boundary whenever the IL evaluation stack is empty
        NOP_BOUNDARIES          = 0x02,     // Before every CEE_NOP instruction
        CALL_SITE_BOUNDARIES    = 0x04,     // Before every CEE_CALL, CEE_CALLVIRT, etc instruction

        // Set of boundaries that debugger should always reasonably ask the JIT for.
        DEFAULT_BOUNDARIES      = STACK_EMPTY_BOUNDARIES | NOP_BOUNDARIES | CALL_SITE_BOUNDARIES
    };

    // Note that SourceTypes can be OR'd together - it's possible that
    // a sequence point will also be a stack_empty point, and/or a call site.
    // The debugger will check to see if a boundary offset's source field &
    // SEQUENCE_POINT is true to determine if the boundary is a sequence point.

    enum SourceTypes
    {
        SOURCE_TYPE_INVALID        = 0x00, // To indicate that nothing else applies
        SEQUENCE_POINT             = 0x01, // The debugger asked for it.
        STACK_EMPTY                = 0x02, // The stack is empty here
        CALL_SITE                  = 0x04, // This is a call site.
        NATIVE_END_OFFSET_UNKNOWN  = 0x08, // Indicates a epilog endpoint
        CALL_INSTRUCTION           = 0x10  // The actual instruction of a call.

    };

    struct OffsetMapping
    {
        DWORD           nativeOffset;
        DWORD           ilOffset;
        SourceTypes     source; // The debugger needs this so that
                                // we don't put Edit and Continue breakpoints where
                                // the stack isn't empty.  We can put regular breakpoints
                                // there, though, so we need a way to discriminate
                                // between offsets.
    };

    /*------------------------------ Var-info -------------------------------*/

    // Note: The debugger needs to target register numbers on platforms other than which the debugger itself
    // is running. To this end it maintains its own values for REGNUM_SP and REGNUM_AMBIENT_SP across multiple
    // platforms. So any change here that may effect these values should be reflected in the definitions
    // contained in debug/inc/DbgIPCEvents.h.
    enum RegNum
    {
#ifdef _TARGET_X86_
        REGNUM_EAX,
        REGNUM_ECX,
        REGNUM_EDX,
        REGNUM_EBX,
        REGNUM_ESP,
        REGNUM_EBP,
        REGNUM_ESI,
        REGNUM_EDI,
#elif _TARGET_ARM_
        REGNUM_R0,
        REGNUM_R1,
        REGNUM_R2,
        REGNUM_R3,
        REGNUM_R4,
        REGNUM_R5,
        REGNUM_R6,
        REGNUM_R7,
        REGNUM_R8,
        REGNUM_R9,
        REGNUM_R10,
        REGNUM_R11,
        REGNUM_R12,
        REGNUM_SP,
        REGNUM_LR,
        REGNUM_PC,
#elif _TARGET_ARM64_
        REGNUM_X0,
        REGNUM_X1,
        REGNUM_X2,
        REGNUM_X3,
        REGNUM_X4,
        REGNUM_X5,
        REGNUM_X6,
        REGNUM_X7,
        REGNUM_X8,
        REGNUM_X9,
        REGNUM_X10,
        REGNUM_X11,
        REGNUM_X12,
        REGNUM_X13,
        REGNUM_X14,
        REGNUM_X15,
        REGNUM_X16,
        REGNUM_X17,
        REGNUM_X18,
        REGNUM_X19,
        REGNUM_X20,
        REGNUM_X21,
        REGNUM_X22,
        REGNUM_X23,
        REGNUM_X24,
        REGNUM_X25,
        REGNUM_X26,
        REGNUM_X27,
        REGNUM_X28,
        REGNUM_FP,
        REGNUM_LR,
        REGNUM_SP,
        REGNUM_PC,
#elif _TARGET_AMD64_
        REGNUM_RAX,
        REGNUM_RCX,
        REGNUM_RDX,
        REGNUM_RBX,
        REGNUM_RSP,
        REGNUM_RBP,
        REGNUM_RSI,
        REGNUM_RDI,
        REGNUM_R8,
        REGNUM_R9,
        REGNUM_R10,
        REGNUM_R11,
        REGNUM_R12,
        REGNUM_R13,
        REGNUM_R14,
        REGNUM_R15,
#else
        PORTABILITY_WARNING("Register numbers not defined on this platform")
#endif
        REGNUM_COUNT,
        REGNUM_AMBIENT_SP, // ambient SP support. Ambient SP is the original SP in the non-BP based frame.
                           // Ambient SP should not change even if there are push/pop operations in the method.

#ifdef _TARGET_X86_
        REGNUM_FP = REGNUM_EBP,
        REGNUM_SP = REGNUM_ESP,
#elif _TARGET_AMD64_
        REGNUM_SP = REGNUM_RSP,
#elif _TARGET_ARM_
#ifdef REDHAWK
        REGNUM_FP = REGNUM_R7,
#else
        REGNUM_FP = REGNUM_R11,
#endif //REDHAWK
#elif _TARGET_ARM64_
        //Nothing to do here. FP is already alloted.
#else
        // RegNum values should be properly defined for this platform
        REGNUM_FP = 0,
        REGNUM_SP = 1,
#endif

    };

    // VarLoc describes the location of a native variable.  Note that currently, VLT_REG_BYREF and VLT_STK_BYREF 
    // are only used for value types on X64.

    enum VarLocType
    {
        VLT_REG,        // variable is in a register
        VLT_REG_BYREF,  // address of the variable is in a register
        VLT_REG_FP,     // variable is in an fp register
        VLT_STK,        // variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_STK_BYREF,  // address of the variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_REG_REG,    // variable lives in two registers
        VLT_REG_STK,    // variable lives partly in a register and partly on the stack
        VLT_STK_REG,    // reverse of VLT_REG_STK
        VLT_STK2,       // variable lives in two slots on the stack
        VLT_FPSTK,      // variable lives on the floating-point stack
        VLT_FIXED_VA,   // variable is a fixed argument in a varargs function (relative to VARARGS_HANDLE)

        VLT_COUNT,
        VLT_INVALID,
#ifdef MDIL
        VLT_MDIL_SYMBOLIC = 0x20
#endif

    };

    struct VarLoc
    {
        VarLocType      vlType;

        union
        {
            // VLT_REG/VLT_REG_FP -- Any pointer-sized enregistered value (TYP_INT, TYP_REF, etc)
            // eg. EAX
            // VLT_REG_BYREF -- the specified register contains the address of the variable
            // eg. [EAX]

            struct
            {
                RegNum      vlrReg;
            } vlReg;

            // VLT_STK -- Any 32 bit value which is on the stack
            // eg. [ESP+0x20], or [EBP-0x28]
            // VLT_STK_BYREF -- the specified stack location contains the address of the variable
            // eg. mov EAX, [ESP+0x20]; [EAX]

            struct
            {
                RegNum      vlsBaseReg;
                signed      vlsOffset;
            } vlStk;

            // VLT_REG_REG -- TYP_LONG with both DWords enregistred
            // eg. RBM_EAXEDX

            struct
            {
                RegNum      vlrrReg1;
                RegNum      vlrrReg2;
            } vlRegReg;

            // VLT_REG_STK -- Partly enregistered TYP_LONG
            // eg { LowerDWord=EAX UpperDWord=[ESP+0x8] }

            struct
            {
                RegNum      vlrsReg;
                struct
                {
                    RegNum      vlrssBaseReg;
                    signed      vlrssOffset;
                }           vlrsStk;
            } vlRegStk;

            // VLT_STK_REG -- Partly enregistered TYP_LONG
            // eg { LowerDWord=[ESP+0x8] UpperDWord=EAX }

            struct
            {
                struct
                {
                    RegNum      vlsrsBaseReg;
                    signed      vlsrsOffset;
                }           vlsrStk;
                RegNum      vlsrReg;
            } vlStkReg;

            // VLT_STK2 -- Any 64 bit value which is on the stack,
            // in 2 successsive DWords.
            // eg 2 DWords at [ESP+0x10]

            struct
            {
                RegNum      vls2BaseReg;
                signed      vls2Offset;
            } vlStk2;

            // VLT_FPSTK -- enregisterd TYP_DOUBLE (on the FP stack)
            // eg. ST(3). Actually it is ST("FPstkHeigth - vpFpStk")

            struct
            {
                unsigned        vlfReg;
            } vlFPstk;

            // VLT_FIXED_VA -- fixed argument of a varargs function.
            // The argument location depends on the size of the variable
            // arguments (...). Inspecting the VARARGS_HANDLE indicates the
            // location of the first arg. This argument can then be accessed
            // relative to the position of the first arg

            struct
            {
                unsigned        vlfvOffset;
            } vlFixedVarArg;

            // VLT_MEMORY

            struct
            {
                void        *rpValue; // pointer to the in-process
                // location of the value.
            } vlMemory;
        };
    };

    // This is used to report implicit/hidden arguments

    enum
    {
        VARARGS_HND_ILNUM   = -1, // Value for the CORINFO_VARARGS_HANDLE varNumber
        RETBUF_ILNUM        = -2, // Pointer to the return-buffer
        TYPECTXT_ILNUM      = -3, // ParamTypeArg for CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG

        UNKNOWN_ILNUM       = -4, // Unknown variable

        MAX_ILNUM           = -4  // Sentinal value. This should be set to the largest magnitude value in th enum
                                  // so that the compression routines know the enum's range.
    };

    struct ILVarInfo
    {
        DWORD           startOffset;
        DWORD           endOffset;
        DWORD           varNumber;
    };

    struct NativeVarInfo
    {
        DWORD           startOffset;
        DWORD           endOffset;
        DWORD           varNumber;
        VarLoc          loc;
    };
};

/**********************************************************************************/
// Some compilers cannot arbitrarily allow the handler nesting level to grow
// arbitrarily during Edit'n'Continue.
// This is the maximum nesting level that a compiler needs to support for EnC

const int MAX_EnC_HANDLER_NESTING_LEVEL = 6;

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

    // return flags (defined above, CORINFO_FLG_PUBLIC ...)
    virtual DWORD getMethodAttribs (
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
             CORINFO_CLASS_HANDLE      memberParent = NULL /* IN */
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
            CORINFO_METHOD_INFO*    info            /* OUT */
            ) = 0;

    // Decides if you have any limitations for inlining. If everything's OK, it will return
    // INLINE_PASS and will fill out pRestrictions with a mask of restrictions the caller of this
    // function must respect. If caller passes pRestrictions = NULL, if there are any restrictions
    // INLINE_FAIL will be returned
    //
    // The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
    //
    // The inlined method need not be verified

    virtual CorInfoInline canInline (
            CORINFO_METHOD_HANDLE       callerHnd,                  /* IN  */
            CORINFO_METHOD_HANDLE       calleeHnd,                  /* IN  */
            DWORD*                      pRestrictions               /* OUT */
            ) = 0;

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
            bool fIsTailPrefix                          /* IN */
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
            unsigned          EHnumber,             /* IN */
            CORINFO_EH_CLAUSE* clause               /* OUT */
            ) = 0;

    // return class it belongs to
    virtual CORINFO_CLASS_HANDLE getMethodClass (
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // return module it belongs to
    virtual CORINFO_MODULE_HANDLE getMethodModule (
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // This function returns the offset of the specified method in the
    // vtable of it's owning class or interface.
    virtual void getMethodVTableOffset (
            CORINFO_METHOD_HANDLE       method,                 /* IN */
            unsigned*                   offsetOfIndirection,    /* OUT */
            unsigned*                   offsetAfterIndirection  /* OUT */
            ) = 0;

    // If a method's attributes have (getMethodAttribs) CORINFO_FLG_INTRINSIC set,
    // getIntrinsicID() returns the intrinsic ID.
    virtual CorInfoIntrinsics getIntrinsicID(
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // Is the given module the System.Numerics.Vectors module?
    // This defaults to false.
    virtual bool isInSIMDModule(
            CORINFO_CLASS_HANDLE        classHnd
            ) { return false; }

    // return the unmanaged calling convention for a PInvoke
    virtual CorInfoUnmanagedCallConv getUnmanagedCallConv(
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // return if any marshaling is required for PInvoke methods.  Note that
    // method == 0 => calli.  The call site sig is only needed for the varargs or calli case
    virtual BOOL pInvokeMarshalingRequired(
            CORINFO_METHOD_HANDLE       method,
            CORINFO_SIG_INFO*           callSiteSig
            ) = 0;

    // Check constraints on method type arguments (only).
    // The parent class should be checked separately using satisfiesClassConstraints(parent).
    virtual BOOL satisfiesMethodConstraints(
            CORINFO_CLASS_HANDLE        parent, // the exact parent of the method
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // Given a delegate target class, a target method parent class,  a  target method,
    // a delegate class, check if the method signature is compatible with the Invoke method of the delegate
    // (under the typical instantiation of any free type variables in the memberref signatures).
    virtual BOOL isCompatibleDelegate(
            CORINFO_CLASS_HANDLE        objCls,           /* type of the delegate target, if any */
            CORINFO_CLASS_HANDLE        methodParentCls,  /* exact parent of the target method, if any */
            CORINFO_METHOD_HANDLE       method,           /* (representative) target method, if any */
            CORINFO_CLASS_HANDLE        delegateCls,      /* exact type of the delegate */
            BOOL                        *pfIsOpenDelegate /* is the delegate open */
            ) = 0;

    // Determines whether the delegate creation obeys security transparency rules
    virtual BOOL isDelegateCreationAllowed (
            CORINFO_CLASS_HANDLE        delegateHnd,
            CORINFO_METHOD_HANDLE       calleeHnd
            ) = 0;


    // Indicates if the method is an instance of the generic
    // method that passes (or has passed) verification
    virtual CorInfoInstantiationVerification isInstantiationOfVerifiedGeneric (
            CORINFO_METHOD_HANDLE   method /* IN  */
            ) = 0;

    // Loads the constraints on a typical method definition, detecting cycles;
    // for use in verification.
    virtual void initConstraintsForVerification(
            CORINFO_METHOD_HANDLE   method, /* IN */
            BOOL *pfHasCircularClassConstraints, /* OUT */
            BOOL *pfHasCircularMethodConstraint /* OUT */
            ) = 0;

    // Returns enum whether the method does not require verification
    // Also see ICorModuleInfo::canSkipVerification
    virtual CorInfoCanSkipVerificationResult canSkipMethodVerification (
            CORINFO_METHOD_HANDLE       ftnHandle
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
            GSCookie * pCookieVal,                     // OUT
            GSCookie ** ppCookieVal                    // OUT
            ) = 0;

#ifdef  MDIL
        virtual unsigned getNumTypeParameters(
            CORINFO_METHOD_HANDLE       method
            ) = 0;

        virtual CorElementType getTypeOfTypeParameter(
            CORINFO_METHOD_HANDLE       method,
            unsigned                    index
            ) = 0;

        virtual CORINFO_CLASS_HANDLE getTypeParameter(
            CORINFO_METHOD_HANDLE       method,
            bool                        classTypeParameter,
            unsigned                    index
            ) = 0;

        virtual unsigned getStructTypeToken(
            InlineContext              *context,
            CORINFO_ARG_LIST_HANDLE     argList
            ) = 0;

        virtual unsigned getEnclosingClassToken(
            InlineContext              *context,
            CORINFO_METHOD_HANDLE       method
            ) = 0;

        virtual CorInfoType getFieldElementType(
            unsigned                    fieldToken, 
            CORINFO_MODULE_HANDLE       scope,
            CORINFO_METHOD_HANDLE       methHnd
            ) = 0;

        // tokens in inlined methods may need to be translated,
        // for example if they are in a generic method we need to fill in type parameters,
        // or in one from another module we need to translate tokens so they are valid
        // in module
        // tokens in dynamic methods (IL stubs) are always translated because
        // as generated they are not backed by any metadata

        // this is called at the start of an inline expansion
        virtual InlineContext *computeInlineContext(
            InlineContext              *outerContext,
            unsigned                    inlinedMethodToken,
            unsigned                    constraintTypeRef,
            CORINFO_METHOD_HANDLE       methHnd
            ) = 0;

        // this does the actual translation
        virtual unsigned translateToken(
            InlineContext              *inlineContext,
            CORINFO_MODULE_HANDLE       scopeHnd,
            unsigned                    token
            ) = 0;

        virtual unsigned getCurrentMethodToken(
            InlineContext              *inlineContext,
            CORINFO_METHOD_HANDLE       method
            ) = 0;

        // computes flags for an IL stub method
        virtual unsigned getStubMethodFlags(
            CORINFO_METHOD_HANDLE method
            ) = 0;
#endif


    /**********************************************************************************/
    //
    // ICorModuleInfo
    //
    /**********************************************************************************/

    // Resolve metadata token into runtime method handles.
    virtual void resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN * pResolvedToken) = 0;

#ifdef MDIL
    // Given a field or method token metaTOK return its parent token
    // we still need this in MDIL, for example for static field access we need the 
    // token of the enclosing type
    virtual unsigned getMemberParent(CORINFO_MODULE_HANDLE  scopeHnd, unsigned metaTOK) = 0;

    // given a token representing an MD array of structs, get the element type token
    virtual unsigned getArrayElementToken(CORINFO_MODULE_HANDLE  scopeHnd, unsigned metaTOK) = 0;
#endif // MDIL

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

    // Returns true if the module does not require verification
    //
    // If fQuickCheckOnlyWithoutCommit=TRUE, the function only checks that the
    // module does not currently require verification in the current AppDomain.
    // This decision could change in the future, and so should not be cached.
    // If it is cached, it should only be used as a hint.
    // This is only used by ngen for calculating certain hints.
    //
   
    // Returns enum whether the module does not require verification
    // Also see ICorMethodInfo::canSkipMethodVerification();
    virtual CorInfoCanSkipVerificationResult canSkipVerification (
            CORINFO_MODULE_HANDLE       module     /* IN  */
            ) = 0;

    // Checks if the given metadata token is valid
    virtual BOOL isValidToken (
            CORINFO_MODULE_HANDLE       module,     /* IN  */
            unsigned                    metaTOK     /* IN  */
            ) = 0;

    // Checks if the given metadata token is valid StringRef
    virtual BOOL isValidStringRef (
            CORINFO_MODULE_HANDLE       module,     /* IN  */
            unsigned                    metaTOK     /* IN  */
            ) = 0;

    virtual BOOL shouldEnforceCallvirtRestriction(
            CORINFO_MODULE_HANDLE   scope
            ) = 0;
#ifdef  MDIL
    virtual unsigned getTypeTokenForFieldOrMethod(
            unsigned                fieldOrMethodToken
            ) = 0;

    virtual unsigned getTokenForType(CORINFO_CLASS_HANDLE  cls) = 0;
#endif

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

    // for completeness
    virtual const char* getClassName (
            CORINFO_CLASS_HANDLE    cls
            ) = 0;


    // Append a (possibly truncated) representation of the type cls to the preallocated buffer ppBuf of length pnBufLen
    // If fNamespace=TRUE, include the namespace/enclosing classes
    // If fFullInst=TRUE (regardless of fNamespace and fAssembly), include namespace and assembly for any type parameters
    // If fAssembly=TRUE, suffix with a comma and the full assembly qualification
    // return size of representation
    virtual int appendClassName(
            __deref_inout_ecount(*pnBufLen) WCHAR** ppBuf, 
            int* pnBufLen,
            CORINFO_CLASS_HANDLE    cls,
            BOOL fNamespace,
            BOOL fFullInst,
            BOOL fAssembly
            ) = 0;

    // Quick check whether the type is a value class. Returns the same value as getClassAttribs(cls) & CORINFO_FLG_VALUECLASS, except faster.
    virtual BOOL isValueClass(CORINFO_CLASS_HANDLE cls) = 0;

    // If this method returns true, JIT will do optimization to inline the check for
    //     GetTypeFromHandle(handle) == obj.GetType()
    virtual BOOL canInlineTypeCheckWithObjectVTable(CORINFO_CLASS_HANDLE cls) = 0;

    // return flags (defined above, CORINFO_FLG_PUBLIC ...)
    virtual DWORD getClassAttribs (
            CORINFO_CLASS_HANDLE    cls
            ) = 0;

    // Returns "TRUE" iff "cls" is a struct type such that return buffers used for returning a value
    // of this type must be stack-allocated.  This will generally be true only if the struct 
    // contains GC pointers, and does not exceed some size limit.  Maintaining this as an invariant allows
    // an optimization: the JIT may assume that return buffer pointers for return types for which this predicate
    // returns TRUE are always stack allocated, and thus, that stores to the GC-pointer fields of such return
    // buffers do not require GC write barriers.
    virtual BOOL isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE cls) = 0;

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
            CORINFO_MODULE_HANDLE *pModule, 
            void **ppIndirection
            ) = 0;

    // return the number of bytes needed by an instance of the class
    virtual unsigned getClassSize (
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    virtual unsigned getClassAlignmentRequirement (
            CORINFO_CLASS_HANDLE        cls,
            BOOL                        fDoubleAlignHint = FALSE
            ) = 0;

    // This is only called for Value classes.  It returns a boolean array
    // in representing of 'cls' from a GC perspective.  The class is
    // assumed to be an array of machine words
    // (of length // getClassSize(cls) / sizeof(void*)),
    // 'gcPtrs' is a poitner to an array of BYTEs of this length.
    // getClassGClayout fills in this array so that gcPtrs[i] is set
    // to one of the CorInfoGCType values which is the GC type of
    // the i-th machine word of an object of type 'cls'
    // returns the number of GC pointers in the array
    virtual unsigned getClassGClayout (
            CORINFO_CLASS_HANDLE        cls,        /* IN */
            BYTE                       *gcPtrs      /* OUT */
            ) = 0;

    // returns the number of instance fields in a class
    virtual unsigned getClassNumInstanceFields (
            CORINFO_CLASS_HANDLE        cls        /* IN */
            ) = 0;

    virtual CORINFO_FIELD_HANDLE getFieldInClass(
            CORINFO_CLASS_HANDLE clsHnd,
            INT num
            ) = 0;

    virtual BOOL checkMethodModifier(
            CORINFO_METHOD_HANDLE hMethod,
            LPCSTR modifier,
            BOOL fOptional
            ) = 0;

    // returns the "NEW" helper optimized for "newCls."
    virtual CorInfoHelpFunc getNewHelper(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_METHOD_HANDLE    callerHandle
            ) = 0;

    // returns the newArr (1-Dim array) helper optimized for "arrayCls."
    virtual CorInfoHelpFunc getNewArrHelper(
            CORINFO_CLASS_HANDLE        arrayCls
            ) = 0;

    // returns the optimized "IsInstanceOf" or "ChkCast" helper
    virtual CorInfoHelpFunc getCastingHelper(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            bool fThrowing
            ) = 0;

    // returns helper to trigger static constructor
    virtual CorInfoHelpFunc getSharedCCtorHelper(
            CORINFO_CLASS_HANDLE clsHnd
            ) = 0;

    virtual CorInfoHelpFunc getSecurityPrologHelper(
            CORINFO_METHOD_HANDLE   ftn
            ) = 0;

    // This is not pretty.  Boxing nullable<T> actually returns
    // a boxed<T> not a boxed Nullable<T>.  This call allows the verifier
    // to call back to the EE on the 'box' instruction and get the transformed
    // type to use for verification.
    virtual CORINFO_CLASS_HANDLE  getTypeForBox(
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
    // a helper that returns a poitner to the unboxed data 
    //     void* unboxHelper(CORINFO_CLASS_HANDLE cls, Object* obj)
    // The EE has the option of NOT returning the copy style helper
    // (But must be able to always honor the non-copy style helper)
    // The EE set 'helperCopies' on return to indicate what kind of
    // helper has been created.  

    virtual CorInfoHelpFunc getUnBoxHelper(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    virtual void getReadyToRunHelper(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CorInfoHelpFunc          id,
            CORINFO_CONST_LOOKUP *   pLookup
            ) = 0;

    virtual const char* getHelperName(
            CorInfoHelpFunc
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
            CORINFO_CONTEXT_HANDLE  context,        // Exact context of method
            BOOL                    speculative = FALSE     // TRUE means don't actually run it
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

    // TRUE if child is a subtype of parent
    // if parent is an interface, then does child implement / extend parent
    virtual BOOL canCast(
            CORINFO_CLASS_HANDLE        child,  // subtype (extends parent)
            CORINFO_CLASS_HANDLE        parent  // base type
            ) = 0;

    // TRUE if cls1 and cls2 are considered equivalent types.
    virtual BOOL areTypesEquivalent(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
            ) = 0;

    // returns is the intersection of cls1 and cls2.
    virtual CORINFO_CLASS_HANDLE mergeClasses(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
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
            CORINFO_CLASS_HANDLE       clsHnd,
            CORINFO_CLASS_HANDLE       *clsRet
            ) = 0;

    // Check constraints on type arguments of this class and parent classes
    virtual BOOL satisfiesClassConstraints(
            CORINFO_CLASS_HANDLE cls
            ) = 0;

    // Check if this is a single dimensional array type
    virtual BOOL isSDArray(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // Get the numbmer of dimensions in an array 
    virtual unsigned getArrayRank(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // Get static field data for an array
    virtual void * getArrayInitializationData(
            CORINFO_FIELD_HANDLE        field,
            DWORD                       size
            ) = 0;

    // Check Visibility rules.
    virtual CorInfoIsAccessAllowedResult canAccessClass(
                        CORINFO_RESOLVED_TOKEN * pResolvedToken,
                        CORINFO_METHOD_HANDLE   callerHandle,
                        CORINFO_HELPER_DESC    *pAccessHelper /* If canAccessMethod returns something other
                                                                 than ALLOWED, then this is filled in. */
                        ) = 0;

    /**********************************************************************************/
    //
    // ICorFieldInfo
    //
    /**********************************************************************************/

    // this function is for debugging only.  It returns the field name
    // and if 'moduleName' is non-null, it sets it to something that will
    // says which method (a class name, or a module name)
    virtual const char* getFieldName (
                        CORINFO_FIELD_HANDLE        ftn,        /* IN */
                        const char                **moduleName  /* OUT */
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
                        CORINFO_FIELD_HANDLE    field,
                        CORINFO_CLASS_HANDLE   *structType,
                        CORINFO_CLASS_HANDLE    memberParent = NULL /* IN */
                        ) = 0;

    // return the data member's instance offset
    virtual unsigned getFieldOffset(
                        CORINFO_FIELD_HANDLE    field
                        ) = 0;

    // TODO: jit64 should be switched to the same plan as the i386 jits - use
    // getClassGClayout to figure out the need for writebarrier helper, and inline the copying.
    // The interpretted value class copy is slow. Once this happens, USE_WRITE_BARRIER_HELPERS
    virtual bool isWriteBarrierHelperRequired(
                        CORINFO_FIELD_HANDLE    field) = 0;

    virtual void getFieldInfo (CORINFO_RESOLVED_TOKEN * pResolvedToken,
                               CORINFO_METHOD_HANDLE  callerHandle,
                               CORINFO_ACCESS_FLAGS   flags,
                               CORINFO_FIELD_INFO    *pResult
                              ) = 0;
#ifdef MDIL
    virtual DWORD getFieldOrdinal(CORINFO_MODULE_HANDLE  tokenScope,
                                            unsigned               fieldToken) = 0;
#endif

    // Returns true iff "fldHnd" represents a static field.
    virtual bool isFieldStatic(CORINFO_FIELD_HANDLE fldHnd) = 0;

    /*********************************************************************************/
    //
    // ICorDebugInfo
    //
    /*********************************************************************************/

    // Query the EE to find out where interesting break points
    // in the code are.  The native compiler will ensure that these places
    // have a corresponding break point in native code.
    //
    // Note that unless CORJIT_FLG_DEBUG_CODE is specified, this function will
    // be used only as a hint and the native compiler should not change its
    // code generation.
    virtual void getBoundaries(
                CORINFO_METHOD_HANDLE   ftn,                // [IN] method of interest
                unsigned int           *cILOffsets,         // [OUT] size of pILOffsets
                DWORD                 **pILOffsets,         // [OUT] IL offsets of interest
                                                            //       jit MUST free with freeArray!
                ICorDebugInfo::BoundaryTypes *implictBoundaries // [OUT] tell jit, all boundries of this type
                ) = 0;

    // Report back the mapping from IL to native code,
    // this map should include all boundaries that 'getBoundaries'
    // reported as interesting to the debugger.

    // Note that debugger (and profiler) is assuming that all of the
    // offsets form a contiguous block of memory, and that the
    // OffsetMapping is sorted in order of increasing native offset.
    virtual void setBoundaries(
                CORINFO_METHOD_HANDLE   ftn,            // [IN] method of interest
                ULONG32                 cMap,           // [IN] size of pMap
                ICorDebugInfo::OffsetMapping *pMap      // [IN] map including all points of interest.
                                                        //      jit allocated with allocateArray, EE frees
                ) = 0;

    // Query the EE to find out the scope of local varables.
    // normally the JIT would trash variables after last use, but
    // under debugging, the JIT needs to keep them live over their
    // entire scope so that they can be inspected.
    //
    // Note that unless CORJIT_FLG_DEBUG_CODE is specified, this function will
    // be used only as a hint and the native compiler should not change its
    // code generation.
    virtual void getVars(
            CORINFO_METHOD_HANDLE           ftn,            // [IN]  method of interest
            ULONG32                        *cVars,          // [OUT] size of 'vars'
            ICorDebugInfo::ILVarInfo       **vars,          // [OUT] scopes of variables of interest
                                                            //       jit MUST free with freeArray!
            bool                           *extendOthers    // [OUT] it TRUE, then assume the scope
                                                            //       of unmentioned vars is entire method
            ) = 0;

    // Report back to the EE the location of every variable.
    // note that the JIT might split lifetimes into different
    // locations etc.

    virtual void setVars(
            CORINFO_METHOD_HANDLE           ftn,            // [IN] method of interest
            ULONG32                         cVars,          // [IN] size of 'vars'
            ICorDebugInfo::NativeVarInfo   *vars            // [IN] map telling where local vars are stored at what points
                                                            //      jit allocated with allocateArray, EE frees
            ) = 0;

    /*-------------------------- Misc ---------------------------------------*/

    // Used to allocate memory that needs to handed to the EE.
    // For eg, use this to allocated memory for reporting debug info,
    // which will be handed to the EE by setVars() and setBoundaries()
    virtual void * allocateArray(
                        ULONG              cBytes
                        ) = 0;

    // JitCompiler will free arrays passed by the EE using this
    // For eg, The EE returns memory in getVars() and getBoundaries()
    // to the JitCompiler, which the JitCompiler should release using
    // freeArray()
    virtual void freeArray(
            void               *array
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
            CORINFO_CLASS_HANDLE       *vcTypeRet       /* OUT */
            ) = 0;

    // If the Arg is a CORINFO_TYPE_CLASS fetch the class handle associated with it
    virtual CORINFO_CLASS_HANDLE getArgClass (
            CORINFO_SIG_INFO*           sig,            /* IN */
            CORINFO_ARG_LIST_HANDLE     args            /* IN */
            ) = 0;

    // Returns type of HFA for valuetype
    virtual CorInfoType getHFAType (
            CORINFO_CLASS_HANDLE hClass
            ) = 0;

 /*****************************************************************************
 * ICorErrorInfo contains methods to deal with SEH exceptions being thrown
 * from the corinfo interface.  These methods may be called when an exception
 * with code EXCEPTION_COMPLUS is caught.
 *****************************************************************************/

    // Returns the HRESULT of the current exception
    virtual HRESULT GetErrorHRESULT(
            struct _EXCEPTION_POINTERS *pExceptionPointers
            ) = 0;

    // Fetches the message of the current exception
    // Returns the size of the message (including terminating null). This can be
    // greater than bufferLength if the buffer is insufficient.
    virtual ULONG GetErrorMessage(
            __inout_ecount(bufferLength) LPWSTR buffer,
            ULONG bufferLength
            ) = 0;

    // returns EXCEPTION_EXECUTE_HANDLER if it is OK for the compile to handle the
    //                        exception, abort some work (like the inlining) and continue compilation
    // returns EXCEPTION_CONTINUE_SEARCH if exception must always be handled by the EE
    //                    things like ThreadStoppedException ...
    // returns EXCEPTION_CONTINUE_EXECUTION if exception is fixed up by the EE

    virtual int FilterException(
            struct _EXCEPTION_POINTERS *pExceptionPointers
            ) = 0;

    // Cleans up internal EE tracking when an exception is caught.
    virtual void HandleException(
            struct _EXCEPTION_POINTERS *pExceptionPointers
            ) = 0;

    virtual void ThrowExceptionForJitResult(
            HRESULT result) = 0;

    //Throws an exception defined by the given throw helper.
    virtual void ThrowExceptionForHelper(
            const CORINFO_HELPER_DESC * throwHelper) = 0;

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
    virtual LPCWSTR getJitTimeLogFilename() = 0;

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

    // this function is for debugging only.  It returns the method name
    // and if 'moduleName' is non-null, it sets it to something that will
    // says which method (a class name, or a module name)
    virtual const char* getMethodName (
            CORINFO_METHOD_HANDLE       ftn,        /* IN */
            const char                **moduleName  /* OUT */
            ) = 0;

    // this function is for debugging only.  It returns a value that
    // is will always be the same for a given method.  It is used
    // to implement the 'jitRange' functionality
    virtual unsigned getMethodHash (
            CORINFO_METHOD_HANDLE       ftn         /* IN */
            ) = 0;

    // this function is for debugging only.
    virtual size_t findNameOfToken (
            CORINFO_MODULE_HANDLE       module,     /* IN  */
            mdToken                     metaTOK,     /* IN  */
            __out_ecount (FQNameCapacity) char * szFQName, /* OUT */
            size_t FQNameCapacity  /* IN */
            ) = 0;

#if COR_JIT_EE_VERSION > 460

    // returns whether the struct is enregisterable. Only valid on a System V VM. Returns true on success, false on failure.
    virtual bool getSystemVAmd64PassStructInRegisterDescriptor(
        /* IN */    CORINFO_CLASS_HANDLE        structHnd,
        /* OUT */   SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr
        ) = 0;

    /*************************************************************************/
    //
    // Configuration values - Allows querying of the CLR configuration.
    //
    /*************************************************************************/

    //  Return an integer ConfigValue if any.
    //
    virtual int getIntConfigValue(
        const wchar_t *name, 
        int defaultValue
        ) = 0;

    //  Return a string ConfigValue if any.
    //
    virtual wchar_t *getStringConfigValue(
        const wchar_t *name
        ) = 0;

    // Free a string ConfigValue returned by the runtime.
    // JITs using the getStringConfigValue query are required
    // to return the string values to the runtime for deletion.
    // this avoid leaking the memory in the JIT.
    virtual void freeStringConfigValue(
        __in_z wchar_t *value
        ) = 0;

#endif // COR_JIT_EE_VERSION

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

    virtual DWORD getThreadTLSIndex(
                    void                  **ppIndirection = NULL
                    ) = 0;

    virtual const void * getInlinedCallFrameVptr(
                    void                  **ppIndirection = NULL
                    ) = 0;

    virtual LONG * getAddrOfCaptureThreadGlobal(
                    void                  **ppIndirection = NULL
                    ) = 0;

    virtual SIZE_T*       getAddrModuleDomainID(CORINFO_MODULE_HANDLE   module) = 0;

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
                              CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY) = 0;

    // return a directly callable address. This can be used similarly to the
    // value returned by getFunctionEntryPoint() except that it is
    // guaranteed to be multi callable entrypoint.
    virtual void getFunctionFixedEntryPoint(
                              CORINFO_METHOD_HANDLE   ftn,
                              CORINFO_CONST_LOOKUP *  pResult) = 0;

    // get the synchronization handle that is passed to monXstatic function
    virtual void* getMethodSync(
                    CORINFO_METHOD_HANDLE               ftn,
                    void                  **ppIndirection = NULL
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
                        BOOL                            fEmbedParent, // TRUE - embeds parent type handle of the field/method handle
                        CORINFO_GENERICHANDLE_RESULT *  pResult) = 0;

    // Return information used to locate the exact enclosing type of the current method.
    // Used only to invoke .cctor method from code shared across generic instantiations
    //   !needsRuntimeLookup       statically known (enclosing type of method itself)
    //   needsRuntimeLookup:
    //      CORINFO_LOOKUP_THISOBJ     use vtable pointer of 'this' param
    //      CORINFO_LOOKUP_CLASSPARAM  use vtable hidden param
    //      CORINFO_LOOKUP_METHODPARAM use enclosing type of method-desc hidden param
    virtual CORINFO_LOOKUP_KIND getLocationOfThisType(
                    CORINFO_METHOD_HANDLE context
                    ) = 0;

    // return the unmanaged target *if method has already been prelinked.*
    virtual void* getPInvokeUnmanagedTarget(
                    CORINFO_METHOD_HANDLE   method,
                    void                  **ppIndirection = NULL
                    ) = 0;

    // return address of fixup area for late-bound PInvoke calls.
    virtual void* getAddressOfPInvokeFixup(
                    CORINFO_METHOD_HANDLE   method,
                    void                  **ppIndirection = NULL
                    ) = 0;

    // Generate a cookie based on the signature that would needs to be passed
    // to CORINFO_HELP_PINVOKE_CALLI
    virtual LPVOID GetCookieForPInvokeCalliSig(
            CORINFO_SIG_INFO* szMetaSig,
            void           ** ppIndirection = NULL
            ) = 0;

    // returns true if a VM cookie can be generated for it (might be false due to cross-module
    // inlining, in which case the inlining should be aborted)
    virtual bool canGetCookieForPInvokeCalliSig(
                    CORINFO_SIG_INFO* szMetaSig
                    ) = 0;

    // Gets a handle that is checked to see if the current method is
    // included in "JustMyCode"
    virtual CORINFO_JUST_MY_CODE_HANDLE getJustMyCodeHandle(
                    CORINFO_METHOD_HANDLE       method,
                    CORINFO_JUST_MY_CODE_HANDLE**ppIndirection = NULL
                    ) = 0;

    // Gets a method handle that can be used to correlate profiling data.
    // This is the IP of a native method, or the address of the descriptor struct
    // for IL.  Always guaranteed to be unique per process, and not to move. */
    virtual void GetProfilingHandle(
                    BOOL                      *pbHookFunction,
                    void                     **pProfilerHandle,
                    BOOL                      *pbIndirectedHandles
                    ) = 0;

    // Returns instructions on how to make the call. See code:CORINFO_CALL_INFO for possible return values.
    virtual void getCallInfo(
                        // Token info
                        CORINFO_RESOLVED_TOKEN * pResolvedToken,

                        //Generics info
                        CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,

                        //Security info
                        CORINFO_METHOD_HANDLE   callerHandle,

                        //Jit info
                        CORINFO_CALLINFO_FLAGS  flags,

                        //out params
                        CORINFO_CALL_INFO       *pResult
                        ) = 0;

    virtual BOOL canAccessFamily(CORINFO_METHOD_HANDLE hCaller,
                                           CORINFO_CLASS_HANDLE hInstanceType) = 0;

    // Returns TRUE if the Class Domain ID is the RID of the class (currently true for every class
    // except reflection emitted classes and generics)
    virtual BOOL isRIDClassDomainID(CORINFO_CLASS_HANDLE cls) = 0;

    // returns the class's domain ID for accessing shared statics
    virtual unsigned getClassDomainID (
                    CORINFO_CLASS_HANDLE    cls,
                    void                  **ppIndirection = NULL
                    ) = 0;


    // return the data's address (for static fields only)
    virtual void* getFieldAddress(
                    CORINFO_FIELD_HANDLE    field,
                    void                  **ppIndirection = NULL
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
    // return the ID (TLS index), which is used to find the begining of the
    // TLS data area for the particular DLL 'field' is associated with.
    virtual DWORD getFieldThreadLocalStoreID (
                    CORINFO_FIELD_HANDLE    field,
                    void                  **ppIndirection = NULL
                    ) = 0;

    // Sets another object to intercept calls to "self" and current method being compiled
    virtual void setOverride(
                ICorDynamicInfo             *pOverride,
                CORINFO_METHOD_HANDLE       currentMethod
                ) = 0;

    // Adds an active dependency from the context method's module to the given module
    // This is internal callback for the EE. JIT should not call it directly.
    virtual void addActiveDependency(
               CORINFO_MODULE_HANDLE       moduleFrom,
               CORINFO_MODULE_HANDLE       moduleTo
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

    // return a thunk that will copy the arguments for the given signature.
    virtual void* getTailCallCopyArgsThunk (
                    CORINFO_SIG_INFO       *pSig,
                    CorInfoHelperTailCallSpecialHandling flags
                    ) = 0;
};

/**********************************************************************************/

// It would be nicer to use existing IMAGE_REL_XXX constants instead of defining our own here...
#define IMAGE_REL_BASED_REL32           0x10
#define IMAGE_REL_BASED_THUMB_BRANCH24  0x13

#endif // _COR_INFO_H_
