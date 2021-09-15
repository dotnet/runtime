// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// FCall.H
//

//
// FCall is a high-performance alternative to ECall. Unlike ECall, FCall
// methods do not necessarily create a frame.   Jitted code calls directly
// to the FCall entry point.   It is possible to do operations that need
// to have a frame within an FCall, you need to manually set up the frame
// before you do such operations.

// It is illegal to cause a GC or EH to happen in an FCALL before setting
// up a frame.  To prevent accidentally violating this rule, FCALLs turn
// on BEGINGCFORBID, which insures that these things can't happen in a
// checked build without causing an ASSERTE.  Once you set up a frame,
// this state is turned off as long as the frame is active, and then is
// turned on again when the frame is torn down.   This mechanism should
// be sufficient to insure that the rules are followed.

// In general you set up a frame by using the following macros

//      HELPER_METHOD_FRAME_BEGIN_RET*()    // Use If the FCALL has a return value
//      HELPER_METHOD_FRAME_BEGIN*()        // Use If FCALL does not return a value
//      HELPER_METHOD_FRAME_END*()

// These macros introduce a scope which is protected by an HelperMethodFrame.
// In this scope you can do EH or GC.   There are rules associated with
// their use.  In particular

//      1) These macros can only be used in the body of a FCALL (that is
//         something using the FCIMPL* or HCIMPL* macros for their decaration.

//      2) You may not perform a 'return' within this scope..

// Compile time errors occur if you try to violate either of these rules.

// The frame that is set up does NOT protect any GC variables (in particular the
// arguments of the FCALL.  Thus you need to do an explicit GCPROTECT once the
// frame is established if you need to protect an argument.  There are flavors
// of HELPER_METHOD_FRAME that protect a certain number of GC variables.  For
// example

//      HELPER_METHOD_FRAME_BEGIN_RET_2(arg1, arg2)

// will protect the GC variables arg1, and arg2 as well as erecting the frame.

// Another invariant that you must be aware of is the need to poll to see if
// a GC is needed by some other thread.   Unless the FCALL is VERY short,
// every code path through the FCALL must do such a poll.  The important
// thing here is that a poll will cause a GC, and thus you can only do it
// when all you GC variables are protected.   To make things easier
// HELPER_METHOD_FRAMES that protect things automatically do this poll.
// If you don't need to protect anything HELPER_METHOD_FRAME_BEGIN_0
// will also do the poll.

// Sometimes it is convenient to do the poll a the end of the frame, you
// can use HELPER_METHOD_FRAME_BEGIN_NOPOLL and HELPER_METHOD_FRAME_END_POLL
// to do the poll at the end.   If somewhere in the middle is the best
// place you can do that too with HELPER_METHOD_POLL()

// You don't need to erect a helper method frame to do a poll.  FC_GC_POLL
// can do this (remember all your GC refs will be trashed).

// Finally if your method is VERY small, you can get away without a poll,
// you have to use FC_GC_POLL_NOT_NEEDED to mark this.
// Use sparingly!

// It is possible to set up the frame as the first operation in the FCALL and
// tear it down as the last operation before returning.  This works and is
// reasonably efficient (as good as an ECall), however, if it is the case that
// you can defer the setup of the frame to an unlikely code path (exception path)
// that is much better.

// If you defer setup of the frame, all codepaths leading to the frame setup
// must be wrapped with PERMIT_HELPER_METHOD_FRAME_BEGIN/END.  These block
// certain compiler optimizations that interfere with the delayed frame setup.
// These macros are automatically included in the HCIMPL, FCIMPL, and frame
// setup macros.

// <TODO>TODO: we should have a way of doing a trial allocation (an allocation that
// will fail if it would cause a GC).  That way even FCALLs that need to allocate
// would not necessarily need to set up a frame.  </TODO>

// It is common to only need to set up a frame in order to throw an exception.
// While this can be done by doing

//      HELPER_METHOD_FRAME_BEGIN()         // Use if FCALL does not return a value
//      COMPlusThrow(execpt);
//      HELPER_METHOD_FRAME_END()

// It is more efficient (in space) to use convenience macro FCTHROW that does
// this for you (sets up a frame, and does the throw).

//      FCTHROW(except)

// Since FCALLS have to conform to the EE calling conventions and not to C
// calling conventions, FCALLS, need to be declared using special macros (FCIMPL*)
// that implement the correct calling conventions.  There are variants of these
// macros depending on the number of args, and sometimes the types of the
// arguments.

//------------------------------------------------------------------------
//    A very simple example:
//
//      FCIMPL2(INT32, Div, INT32 x, INT32 y)
//      {
//          if (y == 0)
//              FCThrow(kDivideByZeroException);
//          return x/y;
//      }
//      FCIMPLEND
//
//
// *** WATCH OUT FOR THESE GOTCHAS: ***
// ------------------------------------
//  - In your FCDECL & FCIMPL protos, don't declare a param as type OBJECTREF
//    or any of its deriveds. This will break on the checked build because
//    __fastcall doesn't enregister C++ objects (which OBJECTREF is).
//    Instead, you need to do something like;
//
//      FCIMPL(.., .., Object* pObject0)
//          OBJECTREF pObject = ObjectToOBJECTREF(pObject0);
//      FCIMPL
//
//    For similar reasons, use Object* rather than OBJECTREF as a return type.
//    Consider either using ObjectToOBJECTREF or calling VALIDATEOBJECTREF
//    to make sure your Object* is valid.
//
//  - FCThrow() must be called directly from your FCall impl function: it
//    cannot be called from a subfunction. Calling from a subfunction breaks
//    the VC code parsing workaround that lets us recover the callee saved registers.
//    Fortunately, you'll get a compile error complaining about an
//    unknown variable "__me".
//
//  - If your FCall returns VOID, you must use FCThrowVoid() rather than
//    FCThrow(). This is because FCThrow() has to generate an unexecuted
//    "return" statement for the code parser.
//
//  - On x86, if first and/or second argument of your FCall cannot be passed
//    in either of the __fastcall registers (ECX/EDX), you must use "V" versions
//    of FCDECL and  FCIMPL macros to enregister arguments correctly. Some of the
//    most common types that fit this requirement are 64-bit values (i.e. INT64 or
//    UINT64) and floating-point values (i.e. FLOAT or DOUBLE). For example, FCDECL3_IVI
//    must be used for FCalls that take 3 arguments and 2nd argument is INT64 and
//    FDECL2_VV must be used for FCalls that take 2 arguments where both are FLOAT.
//
//  - You may use structs for protecting multiple OBJECTREF's simultaneously.
//    In these cases, you must use a variant of a helper method frame with PROTECT
//    in the name, to ensure all the OBJECTREF's in the struct get protected.
//    Also, initialize all the OBJECTREF's first.  Like this:
//
//    FCIMPL4(Object*, COMNlsInfo::nativeChangeCaseString, LocaleIDObject* localeUNSAFE,
//            INT_PTR pNativeTextInfo, StringObject* pStringUNSAFE, CLR_BOOL bIsToUpper)
//    {
//      [ignoring CONTRACT for now]
//      struct _gc
//      {
//          STRINGREF pResult;
//          STRINGREF pString;
//          LOCALEIDREF pLocale;
//      } gc;
//      gc.pResult = NULL;
//      gc.pString = ObjectToSTRINGREF(pStringUNSAFE);
//      gc.pLocale = (LOCALEIDREF)ObjectToOBJECTREF(localeUNSAFE);
//
//      HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc)
//
//    If you forgot the PROTECT part, the macro will only protect the first OBJECTREF,
//    introducing a subtle GC hole in your code.  Fortunately, we now issue a
//    compile-time error if you forget.

// How FCall works:
// ----------------
//   An FCall target uses __fastcall or some other calling convention to
//   match the IL calling convention exactly. Thus, a call to FCall is a direct
//   call to the target w/ no intervening stub or frame.
//
//   The tricky part is when FCThrow is called. FCThrow must generate
//   a proper method frame before allocating and throwing the exception.
//   To do this, it must recover several things:
//
//      - The location of the FCIMPL's return address (since that's
//        where the frame will be based.)
//
//      - The on-entry values of the callee-saved regs; which must
//        be recorded in the frame so that GC can update them.
//        Depending on how VC compiles your FCIMPL, those values are still
//        in the original registers or saved on the stack.
//
//        To figure out which, FCThrow() generates the code:
//
//              while (NULL == __FCThrow(__me, ...)) {};
//              return 0;
//
//        The "return" statement will never execute; but its presence guarantees
//        that VC will follow the __FCThrow() call with a VC epilog
//        that restores the callee-saved registers using a pretty small
//        and predictable set of Intel opcodes. __FCThrow() parses this
//        epilog and simulates its execution to recover the callee saved
//        registers.
//
//        The while loop is to prevent the compiler from doing tail call optimizations.
//        The helper frame interpretter needs the frame to be present.
//
//      - The MethodDesc* that this FCall implements. This MethodDesc*
//        is part of the frame and ensures that the FCall will appear
//        in the exception's stack trace. To get this, FCDECL declares
//        a static local __me, initialized to point to the FC target itself.
//        This address is exactly what's stored in the ECall lookup tables;
//        so __FCThrow() simply does a reverse lookup on that table to recover
//        the MethodDesc*.
//


#ifndef __FCall_h__
#define __FCall_h__

#include "gms.h"
#include "runtimeexceptionkind.h"
#include "debugreturn.h"

//==============================================================================================
// These macros defeat compiler optimizations that might mix nonvolatile
// register loads and stores with other code in the function body.  This
// creates problems for the frame setup code, which assumes that any
// nonvolatiles that are saved at the point of the frame setup will be
// re-loaded when the frame is popped.
//
// Currently this is only known to be an issue on AMD64.  It's uncertain
// whether it is an issue on x86.
//==============================================================================================

#if defined(TARGET_AMD64) && !defined(TARGET_UNIX)

//
// On AMD64 this is accomplished by including a setjmp anywhere in a function.
// Doesn't matter whether it is reachable or not, and in fact in optimized
// builds the setjmp is removed altogether.
//
#include <setjmp.h>

//
// Use of setjmp is temporary, we will eventually have compiler intrinsics to
// disable the optimizations.  Besides, we don't actually execute setjmp in
// these macros (or anywhere else in the VM on AMD64).
//
#pragma warning(disable:4611) // interaction between '_setjmp' and C++ object destruction is non-portable

#ifdef _DEBUG
//
// Linked list of unmanaged methods preceeding a HelperMethodFrame push.  This
// is linked onto the current Thread.  Each list entry is stack-allocated so it
// can be associated with an unmanaged frame.  Each unmanaged frame needs to be
// associated with at least one list entry.
//
struct HelperMethodFrameCallerList
{
    HelperMethodFrameCallerList *pCaller;
};
#endif // _DEBUG

//
// Resets the Thread state at a new managed -> fcall transition.
//
class FCallTransitionState
{
public:

    FCallTransitionState () NOT_DEBUG({ LIMITED_METHOD_CONTRACT; });
    ~FCallTransitionState () NOT_DEBUG({ LIMITED_METHOD_CONTRACT; });

#ifdef _DEBUG
private:
    Thread *m_pThread;
    HelperMethodFrameCallerList *m_pPreviousHelperMethodFrameCallerList;
#endif // _DEBUG
};

//
// Pushes/pops state for each caller.
//
class PermitHelperMethodFrameState
{
public:

    PermitHelperMethodFrameState () NOT_DEBUG({ LIMITED_METHOD_CONTRACT; });
    ~PermitHelperMethodFrameState () NOT_DEBUG({ LIMITED_METHOD_CONTRACT; });

    static VOID CheckHelperMethodFramePermitted () NOT_DEBUG({ LIMITED_METHOD_CONTRACT; });

#ifdef _DEBUG
private:
    Thread *m_pThread;
    HelperMethodFrameCallerList m_ListEntry;
#endif // _DEBUG
};

//
// Resets the Thread state after the HelperMethodFrame is pushed.  At this
// point, the HelperMethodFrame is capable of unwinding to the managed code,
// so we can reset the Thread state for any nested fcalls.
//
class CompletedFCallTransitionState
{
public:

    CompletedFCallTransitionState () NOT_DEBUG({ LIMITED_METHOD_CONTRACT; });
    ~CompletedFCallTransitionState () NOT_DEBUG({ LIMITED_METHOD_CONTRACT; });

#ifdef _DEBUG
private:

    HelperMethodFrameCallerList *m_pLastHelperMethodFrameCallerList;
#endif // _DEBUG
};

#define PERMIT_HELPER_METHOD_FRAME_BEGIN()                                  \
        if (1)                                                              \
        {                                                                   \
            PermitHelperMethodFrameState ___PermitHelperMethodFrameState;

#define PERMIT_HELPER_METHOD_FRAME_END()    \
        }                                   \
        else                                \
        {                                   \
            jmp_buf ___jmpbuf;              \
            setjmp(___jmpbuf);              \
            __assume(0);                    \
        }

#define FCALL_TRANSITION_BEGIN()                        \
        FCallTransitionState ___FCallTransitionState;   \
        PERMIT_HELPER_METHOD_FRAME_BEGIN();

#define FCALL_TRANSITION_END()              \
        PERMIT_HELPER_METHOD_FRAME_END();

#define CHECK_HELPER_METHOD_FRAME_PERMITTED() \
        PermitHelperMethodFrameState::CheckHelperMethodFramePermitted(); \
        CompletedFCallTransitionState ___CompletedFCallTransitionState;

#else // unsupported processor

#define PERMIT_HELPER_METHOD_FRAME_BEGIN()
#define PERMIT_HELPER_METHOD_FRAME_END()
#define FCALL_TRANSITION_BEGIN()
#define FCALL_TRANSITION_END()
#define CHECK_HELPER_METHOD_FRAME_PERMITTED()

#endif // unsupported processor

//==============================================================================================
// This is where FCThrow ultimately ends up. Never call this directly.
// Use the FCThrow() macros. __FCThrowArgument is the helper to throw ArgumentExceptions
// with a resource taken from the managed resource manager.
//==============================================================================================
LPVOID __FCThrow(LPVOID me, enum RuntimeExceptionKind reKind, UINT resID, LPCWSTR arg1, LPCWSTR arg2, LPCWSTR arg3);
LPVOID __FCThrowArgument(LPVOID me, enum RuntimeExceptionKind reKind, LPCWSTR argumentName, LPCWSTR resourceName);

//==============================================================================================
// FDECLn: A set of macros for generating header declarations for FC targets.
// Use FIMPLn for the actual body.
//==============================================================================================

// Note: on the x86, these defs reverse all but the first two arguments
// (IL stack calling convention is reversed from __fastcall.)


// Calling convention for varargs
#define F_CALL_VA_CONV __cdecl


#ifdef TARGET_X86

// Choose the appropriate calling convention for FCALL helpers on the basis of the JIT calling convention
#ifdef __GNUC__
#define F_CALL_CONV __attribute__((cdecl, regparm(3)))

// GCC FCALL convention (simulated via cdecl, regparm(3)) is different from MSVC FCALL convention. GCC can use up
// to 3 registers to store parameters. The registers used are EAX, EDX, ECX. Dummy parameters and reordering
// of the actual parameters in the FCALL signature is used to make the calling convention to look like in MSVC.
#define SWIZZLE_REGARG_ORDER
#else // __GNUC__
#define F_CALL_CONV __fastcall
#endif // !__GNUC__

#define SWIZZLE_STKARG_ORDER
#else // TARGET_X86

//
// non-x86 platforms don't have messed-up calling convention swizzling
//
#define F_CALL_CONV
#endif // !TARGET_X86

#ifdef SWIZZLE_STKARG_ORDER
#ifdef SWIZZLE_REGARG_ORDER

#define FCDECL0(rettype, funcname) rettype F_CALL_CONV funcname()
#define FCDECL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1)
#define FCDECL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a1)
#define FCDECL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1)
#define FCDECL2VA(rettype, funcname, a1, a2) rettype F_CALL_VA_CONV funcname(a1, a2, ...)
#define FCDECL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a2, a1)
#define FCDECL2_VI(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a2, a1)
#define FCDECL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1, a2)
#define FCDECL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3)
#define FCDECL3_IIV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3)
#define FCDECL3_VII(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a3, a2, a1)
#define FCDECL3_IVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1, a3, a2)
#define FCDECL3_IVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a3, a1, a2)
#define FCDECL3_VVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a3, a2, a1)
#define FCDECL3_VVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a3, a2, a1)
#define FCDECL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a4, a3)
#define FCDECL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a5, a4, a3)
#define FCDECL6(rettype, funcname, a1, a2, a3, a4, a5, a6) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a6, a5, a4, a3)
#define FCDECL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a7, a6, a5, a4, a3)
#define FCDECL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a8, a7, a6, a5, a4, a3)
#define FCDECL9(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL10(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL11(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL12(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL13(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL14(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a14, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3)

#define FCDECL5_IVI(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(int /* EAX */, a3, a1, a5, a4, a2)
#define FCDECL5_VII(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(int /* EAX */, a3, a2, a5, a4, a1)

#else // SWIZZLE_REGARG_ORDER

#define FCDECL0(rettype, funcname) rettype F_CALL_CONV funcname()
#define FCDECL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
#define FCDECL2VA(rettype, funcname, a1, a2) rettype F_CALL_VA_CONV funcname(a1, a2, ...)
#define FCDECL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a2, a1)
#define FCDECL2_VI(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a2, a1)
#define FCDECL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
#define FCDECL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_IIV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_VII(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a2, a3, a1)
#define FCDECL3_IVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a3, a2)
#define FCDECL3_IVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a3, a2)
#define FCDECL3_VVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a2, a1, a3)
#define FCDECL3_VVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a3, a2, a1)
#define FCDECL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(a1, a2, a4, a3)
#define FCDECL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a1, a2, a5, a4, a3)
#define FCDECL6(rettype, funcname, a1, a2, a3, a4, a5, a6) rettype F_CALL_CONV funcname(a1, a2, a6, a5, a4, a3)
#define FCDECL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) rettype F_CALL_CONV funcname(a1, a2, a7, a6, a5, a4, a3)
#define FCDECL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) rettype F_CALL_CONV funcname(a1, a2, a8, a7, a6, a5, a4, a3)
#define FCDECL9(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9) rettype F_CALL_CONV funcname(a1, a2, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL10(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) rettype F_CALL_CONV funcname(a1, a2, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL11(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) rettype F_CALL_CONV funcname(a1, a2, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL12(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) rettype F_CALL_CONV funcname(a1, a2, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL13(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) rettype F_CALL_CONV funcname(a1, a2, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCDECL14(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) rettype F_CALL_CONV funcname(a1, a2, a14, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3)

#define FCDECL5_IVI(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a1, a3, a5, a4, a2)
#define FCDECL5_VII(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a2, a3, a5, a4, a1)

#endif // !SWIZZLE_REGARG_ORDER

#if 0
//
// don't use something like this... directly calling an FCALL from within the runtime breaks stackwalking because
// the FCALL reverse mapping only gets established in ECall::GetFCallImpl and that codepath is circumvented by
// directly calling and FCALL
// See below for usage of FC_CALL_INNER (used in SecurityStackWalk::Check presently)
//
#define FCCALL0(funcname) funcname()
#define FCCALL1(funcname, a1) funcname(a1)
#define FCCALL2(funcname, a1, a2) funcname(a1, a2)
#define FCCALL3(funcname, a1, a2, a3) funcname(a1, a2, a3)
#define FCCALL4(funcname, a1, a2, a3, a4) funcname(a1, a2, a4, a3)
#define FCCALL5(funcname, a1, a2, a3, a4, a5) funcname(a1, a2, a5, a4, a3)
#define FCCALL6(funcname, a1, a2, a3, a4, a5, a6) funcname(a1, a2, a6, a5, a4, a3)
#define FCCALL7(funcname, a1, a2, a3, a4, a5, a6, a7) funcname(a1, a2, a7, a6, a5, a4, a3)
#define FCCALL8(funcname, a1, a2, a3, a4, a5, a6, a7, a8) funcname(a1, a2, a8, a7, a6, a5, a4, a3)
#define FCCALL9(funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9) funcname(a1, a2, a9, a8, a7, a6, a5, a4, a3)
#define FCCALL10(funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) funcname(a1, a2, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCCALL11(funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) funcname(a1, a2, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#define FCCALL12(funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) funcname(a1, a2, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3)
#endif // 0

#else // !SWIZZLE_STKARG_ORDER

#define FCDECL0(rettype, funcname) rettype F_CALL_CONV funcname()
#define FCDECL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
#define FCDECL2VA(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2, ...)
#define FCDECL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
#define FCDECL2_VI(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
#define FCDECL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
#define FCDECL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_IIV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_VII(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_IVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_IVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_VVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL3_VVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3)
#define FCDECL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(a1, a2, a3, a4)
#define FCDECL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5)
#define FCDECL6(rettype, funcname, a1, a2, a3, a4, a5, a6) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6)
#define FCDECL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7)
#define FCDECL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7, a8)
#define FCDECL9(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9)
#define FCDECL10(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10)
#define FCDECL11(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11)
#define FCDECL12(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12)
#define FCDECL13(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13)
#define FCDECL14(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14)

#define FCDECL5_IVI(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5)
#define FCDECL5_VII(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5)

#endif // !SWIZZLE_STKARG_ORDER

#define HELPER_FRAME_DECL(x) FrameWithCookie<HelperMethodFrame_##x##OBJ> __helperframe

// use the capture state machinery if the architecture has one
//
// For a normal build we create a loop (see explaination on RestoreState below)
// We don't want a loop here for PREFAST since that causes
//   warning 263: Using _alloca in a loop
// And we can't use DEBUG_OK_TO_RETURN for PREFAST because the PREFAST version
// requires that you already be in a DEBUG_ASSURE_NO_RETURN_BEGIN scope

#define HelperMethodFrame_0OBJ      HelperMethodFrame
#define HELPER_FRAME_ARGS(attribs)  __me, attribs
#define FORLAZYMACHSTATE(x) x

#if defined(_PREFAST_)
  #define FORLAZYMACHSTATE_BEGINLOOP(x) x
  #define FORLAZYMACHSTATE_ENDLOOP(x)
  #define FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_BEGIN
  #define FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_END
#else
  #define FORLAZYMACHSTATE_BEGINLOOP(x) x do
  #define FORLAZYMACHSTATE_ENDLOOP(x) while(x)
  #define FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_BEGIN  DEBUG_OK_TO_RETURN_BEGIN(LAZYMACHSTATE)
  #define FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_END    DEBUG_OK_TO_RETURN_END(LAZYMACHSTATE)
#endif

// BEGIN: before gcpoll
//FCallGCCanTriggerNoDtor __fcallGcCanTrigger;
//__fcallGcCanTrigger.Enter();

// END: after gcpoll
//__fcallGcCanTrigger.Leave(__FUNCTION__, __FILE__, __LINE__);

// We have to put DEBUG_OK_TO_RETURN_BEGIN around the FORLAZYMACHSTATE
// to allow the HELPER_FRAME to be installed inside an SO_INTOLERANT region
// which does not allow a return.  The return is used by FORLAZYMACHSTATE
// to capture the state, but is not an actual return, so it is ok.
#define HELPER_METHOD_FRAME_BEGIN_EX_BODY(ret, helperFrame, gcpoll, allowGC)  \
        FORLAZYMACHSTATE_BEGINLOOP(int alwaysZero = 0;)         \
        {                                                       \
            INDEBUG(static BOOL __haveCheckedRestoreState = FALSE;)     \
            PERMIT_HELPER_METHOD_FRAME_BEGIN();                 \
            CHECK_HELPER_METHOD_FRAME_PERMITTED();              \
            helperFrame;                                        \
            FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_BEGIN;          \
            FORLAZYMACHSTATE(CAPTURE_STATE(__helperframe.MachineState(), ret);) \
            FORLAZYMACHSTATE_DEBUG_OK_TO_RETURN_END;            \
            INDEBUG(__helperframe.SetAddrOfHaveCheckedRestoreState(&__haveCheckedRestoreState)); \
            DEBUG_ASSURE_NO_RETURN_BEGIN(HELPER_METHOD_FRAME);  \
            INCONTRACT(FCallGCCanTrigger::Enter());

#define HELPER_METHOD_FRAME_BEGIN_EX(ret, helperFrame, gcpoll, allowGC)         \
        HELPER_METHOD_FRAME_BEGIN_EX_BODY(ret, helperFrame, gcpoll, allowGC)    \
            /* <TODO>TODO TURN THIS ON!!!   </TODO> */                    \
            /* gcpoll; */                                                       \
            INSTALL_MANAGED_EXCEPTION_DISPATCHER;                               \
            __helperframe.Push();                                               \
            MAKE_CURRENT_THREAD_AVAILABLE_EX(__helperframe.GetThread()); \
            INSTALL_UNWIND_AND_CONTINUE_HANDLER_FOR_HMF(&__helperframe);

#define HELPER_METHOD_FRAME_BEGIN_EX_NOTHROW(ret, helperFrame, gcpoll, allowGC, probeFailExpr) \
        HELPER_METHOD_FRAME_BEGIN_EX_BODY(ret, helperFrame, gcpoll, allowGC)    \
            __helperframe.Push();                                         \
            MAKE_CURRENT_THREAD_AVAILABLE_EX(__helperframe.GetThread()); \
            /* <TODO>TODO TURN THIS ON!!!   </TODO> */                    \
            /* gcpoll; */

// The while(__helperframe.RestoreState() needs a bit of explanation.
// The issue is insuring that the same machine state (which registers saved)
// exists when the machine state is probed (when the frame is created, and
// when it is actually used (when the frame is popped.  We do this by creating
// a flow of control from use to def.  Note that 'RestoreState' always returns false
// we never actually loop, but the compiler does not know that, and thus
// will be forced to make the keep the state of register spills the same at
// the two locations.

#define HELPER_METHOD_FRAME_END_EX_BODY(gcpoll,allowGC) \
            /* <TODO>TODO TURN THIS ON!!!   </TODO> */                \
            /* gcpoll; */                                                   \
            DEBUG_ASSURE_NO_RETURN_END(HELPER_METHOD_FRAME);                \
            INCONTRACT(FCallGCCanTrigger::Leave(__FUNCTION__, __FILE__, __LINE__)); \
            FORLAZYMACHSTATE(alwaysZero =                                   \
            HelperMethodFrameRestoreState(INDEBUG_COMMA(&__helperframe)     \
                                          __helperframe.MachineState());)   \
            PERMIT_HELPER_METHOD_FRAME_END()                                \
        } FORLAZYMACHSTATE_ENDLOOP(alwaysZero);

#define HELPER_METHOD_FRAME_END_EX(gcpoll,allowGC)                          \
            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;                          \
            __helperframe.Pop();                                            \
            UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;                         \
        HELPER_METHOD_FRAME_END_EX_BODY(gcpoll,allowGC);

#define HELPER_METHOD_FRAME_END_EX_NOTHROW(gcpoll,allowGC)                  \
            __helperframe.Pop();                                            \
        HELPER_METHOD_FRAME_END_EX_BODY(gcpoll,allowGC);

#define HELPER_METHOD_FRAME_BEGIN_ATTRIB(attribs)                                       \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return,                                                                     \
            HELPER_FRAME_DECL(0)(HELPER_FRAME_ARGS(attribs)),                           \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_0()                                                   \
        HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_NONE)

#define HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(attribs)                                \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return,                                                                     \
            HELPER_FRAME_DECL(0)(HELPER_FRAME_ARGS(attribs)),                           \
            {},FALSE)

#define HELPER_METHOD_FRAME_BEGIN_NOPOLL() HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_NONE)

#define HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(attribs, arg1)                               \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return,                                                                     \
            HELPER_FRAME_DECL(1)(HELPER_FRAME_ARGS(attribs),                            \
                (OBJECTREF*) &arg1),                                                    \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_1(arg1)  HELPER_METHOD_FRAME_BEGIN_ATTRIB_1(Frame::FRAME_ATTR_NONE, arg1)

#define HELPER_METHOD_FRAME_BEGIN_ATTRIB_2(attribs, arg1, arg2)                         \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        static_assert(sizeof(arg2) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return,                                                                     \
            HELPER_FRAME_DECL(2)(HELPER_FRAME_ARGS(attribs),                            \
                (OBJECTREF*) &arg1, (OBJECTREF*) &arg2),                                \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_2(arg1, arg2) HELPER_METHOD_FRAME_BEGIN_ATTRIB_2(Frame::FRAME_ATTR_NONE, arg1, arg2)

#define HELPER_METHOD_FRAME_BEGIN_ATTRIB_3(attribs, arg1, arg2, arg3)                         \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        static_assert(sizeof(arg2) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        static_assert(sizeof(arg3) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return,                                                                     \
            HELPER_FRAME_DECL(3)(HELPER_FRAME_ARGS(attribs),                            \
                (OBJECTREF*) &arg1, (OBJECTREF*) &arg2, (OBJECTREF*) &arg3),                                \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_3(arg1, arg2, arg3) HELPER_METHOD_FRAME_BEGIN_ATTRIB_3(Frame::FRAME_ATTR_NONE, arg1, arg2, arg3)

#define HELPER_METHOD_FRAME_BEGIN_PROTECT(gc)                                           \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return,                                                                     \
            HELPER_FRAME_DECL(PROTECT)(HELPER_FRAME_ARGS(Frame::FRAME_ATTR_NONE),       \
                (OBJECTREF*)&(gc), sizeof(gc)/sizeof(OBJECTREF)),                       \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_NOPOLL(attribs)                            \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return 0,                                                                   \
            HELPER_FRAME_DECL(0)(HELPER_FRAME_ARGS(attribs)),                           \
            {},FALSE)

#define HELPER_METHOD_FRAME_BEGIN_RET_VC_ATTRIB_NOPOLL(attribs)                         \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            FC_RETURN_VC(),                                                             \
            HELPER_FRAME_DECL(0)(HELPER_FRAME_ARGS(attribs)),                           \
            {},FALSE)

#define HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(attribs)                                   \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return 0,                                                                   \
            HELPER_FRAME_DECL(0)(HELPER_FRAME_ARGS(attribs)),                           \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_0()                                               \
        HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_NONE)

#define HELPER_METHOD_FRAME_BEGIN_RET_VC_0()                                            \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            FC_RETURN_VC(),                                                             \
            HELPER_FRAME_DECL(0)(HELPER_FRAME_ARGS(Frame::FRAME_ATTR_NONE)),            \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(attribs, arg1)                           \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return 0,                                                                   \
            HELPER_FRAME_DECL(1)(HELPER_FRAME_ARGS(attribs),                            \
                (OBJECTREF*) &arg1),                                                    \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_NOTHROW_1(probeFailExpr, arg1)                    \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX_NOTHROW(                                           \
            return 0,                                                                   \
            HELPER_FRAME_DECL(1)(HELPER_FRAME_ARGS(Frame::FRAME_ATTR_NO_THREAD_ABORT),  \
                (OBJECTREF*) &arg1),                                                    \
            HELPER_METHOD_POLL(), TRUE, probeFailExpr)

#define HELPER_METHOD_FRAME_BEGIN_RET_VC_ATTRIB_1(attribs, arg1)                        \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            FC_RETURN_VC(),                                                             \
            HELPER_FRAME_DECL(1)(HELPER_FRAME_ARGS(attribs),                            \
                (OBJECTREF*) &arg1),                                                    \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(attribs, arg1, arg2)                     \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        static_assert(sizeof(arg2) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return 0,                                                                   \
            HELPER_FRAME_DECL(2)(HELPER_FRAME_ARGS(attribs),                            \
                (OBJECTREF*) &arg1, (OBJECTREF*) &arg2),                                \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_VC_ATTRIB_2(attribs, arg1, arg2)                  \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        static_assert(sizeof(arg2) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            FC_RETURN_VC(),                                                             \
            HELPER_FRAME_DECL(2)(HELPER_FRAME_ARGS(attribs),                            \
                (OBJECTREF*) &arg1, (OBJECTREF*) &arg2),                                \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_PROTECT(attribs, gc)                       \
        HELPER_METHOD_FRAME_BEGIN_EX(                                                   \
            return 0,                                                                   \
            HELPER_FRAME_DECL(PROTECT)(HELPER_FRAME_ARGS(attribs),                      \
                (OBJECTREF*)&(gc), sizeof(gc)/sizeof(OBJECTREF)),                       \
            HELPER_METHOD_POLL(),TRUE)

#define HELPER_METHOD_FRAME_BEGIN_RET_VC_NOPOLL()                                       \
        HELPER_METHOD_FRAME_BEGIN_RET_VC_ATTRIB_NOPOLL(Frame::FRAME_ATTR_NONE)

#define HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL()                                          \
        HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_NOPOLL(Frame::FRAME_ATTR_NONE)

#define HELPER_METHOD_FRAME_BEGIN_RET_1(arg1)                                           \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_NONE, arg1)

#define HELPER_METHOD_FRAME_BEGIN_RET_VC_1(arg1)                                        \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_RET_VC_ATTRIB_1(Frame::FRAME_ATTR_NONE, arg1)

#define HELPER_METHOD_FRAME_BEGIN_RET_2(arg1, arg2)                                     \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        static_assert(sizeof(arg2) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(Frame::FRAME_ATTR_NONE, arg1, arg2)

#define HELPER_METHOD_FRAME_BEGIN_RET_VC_2(arg1, arg2)                                  \
        static_assert(sizeof(arg1) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        static_assert(sizeof(arg2) == sizeof(OBJECTREF), "GC protecting structs of multiple OBJECTREFs requires a PROTECT variant of the HELPER METHOD FRAME macro");\
        HELPER_METHOD_FRAME_BEGIN_RET_VC_ATTRIB_2(Frame::FRAME_ATTR_NONE, arg1, arg2)

#define HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc)                                       \
        HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_PROTECT(Frame::FRAME_ATTR_NONE, gc)


#define HELPER_METHOD_FRAME_END()        HELPER_METHOD_FRAME_END_EX({},FALSE)
#define HELPER_METHOD_FRAME_END_POLL()   HELPER_METHOD_FRAME_END_EX(HELPER_METHOD_POLL(),TRUE)
#define HELPER_METHOD_FRAME_END_NOTHROW()HELPER_METHOD_FRAME_END_EX_NOTHROW({},FALSE)

// This is the fastest way to do a GC poll if you have already erected a HelperMethodFrame
#define HELPER_METHOD_POLL()            { __helperframe.Poll(); INCONTRACT(__fCallCheck.SetDidPoll()); }

// The HelperMethodFrame knows how to get its return address.  Let other code get at it, too.
//  (Uses comma operator to call InsureInit & discard result.
#define HELPER_METHOD_FRAME_GET_RETURN_ADDRESS()                                        \
    ( static_cast<UINT_PTR>( (__helperframe.InsureInit(false, NULL)), (__helperframe.MachineState()->GetRetAddr()) ) )

    // Very short routines, or routines that are guarenteed to force GC or EH
    // don't need to poll the GC.  USE VERY SPARINGLY!!!
#define FC_GC_POLL_NOT_NEEDED()    INCONTRACT(__fCallCheck.SetNotNeeded())

Object* FC_GCPoll(void* me, Object* objToProtect = NULL);

#define FC_GC_POLL_EX(ret)                                  \
    {                                                       \
        INCONTRACT(Thread::TriggersGC(GetThread());)        \
        INCONTRACT(__fCallCheck.SetDidPoll();)              \
        if (g_TrapReturningThreads.LoadWithoutBarrier())    \
        {                                                   \
            if (FC_GCPoll(__me))                            \
                return ret;                                 \
            while (0 == FC_NO_TAILCALL) { }; /* side effect the compile can't remove */  \
        }                                                   \
    }

#define FC_GC_POLL()        FC_GC_POLL_EX(;)
#define FC_GC_POLL_RET()    FC_GC_POLL_EX(0)

#define FC_GC_POLL_AND_RETURN_OBJREF(obj)                   \
    {                                                       \
        INCONTRACT(__fCallCheck.SetDidPoll();)              \
        Object* __temp = OBJECTREFToObject(obj);            \
        if (g_TrapReturningThreads.LoadWithoutBarrier())    \
        {                                                   \
            __temp = FC_GCPoll(__me, __temp);               \
            while (0 == FC_NO_TAILCALL) { }; /* side effect the compile can't remove */  \
        }                                                   \
        return __temp;                                      \
    }

#if defined(ENABLE_CONTRACTS)
#define FC_CAN_TRIGGER_GC()         FCallGCCanTrigger::Enter()
#define FC_CAN_TRIGGER_GC_END()     FCallGCCanTrigger::Leave(__FUNCTION__, __FILE__, __LINE__)

#define FC_CAN_TRIGGER_GC_HAVE_THREAD(thread)       FCallGCCanTrigger::Enter(thread)
#define FC_CAN_TRIGGER_GC_HAVE_THREADEND(thread)    FCallGCCanTrigger::Leave(thread, __FUNCTION__, __FILE__, __LINE__)

    // turns on forbidGC for the lifetime of the instance
class ForbidGC {
protected:
    Thread *m_pThread;
public:
    ForbidGC(const char *szFile, int lineNum);
    ~ForbidGC();
};

    // this little helper class checks to make certain
    // 1) ForbidGC is set throughout the routine.
    // 2) Sometime during the routine, a GC poll is done

class FCallCheck : public ForbidGC {
public:
    FCallCheck(const char *szFile, int lineNum);
    ~FCallCheck();
    void SetDidPoll()       {LIMITED_METHOD_CONTRACT;  didGCPoll = true; }
    void SetNotNeeded()     {LIMITED_METHOD_CONTRACT;  notNeeded = true; }

private:
#ifdef _DEBUG
    DWORD         unbreakableLockCount;
#endif
    bool          didGCPoll;            // GC poll was done
    bool          notNeeded;            // GC poll not needed
    unsigned __int64 startTicks;        // tick count at beginning of FCall
};

        // FC_COMMON_PROLOG is used for both FCalls and HCalls
#define FC_COMMON_PROLOG(target, assertFn)      \
        /* The following line has to be first.  We do not want to trash last error */ \
        DWORD __lastError = ::GetLastError();   \
        static void* __cache = 0;               \
        assertFn(__cache, (LPVOID)target);      \
        {                                       \
            Thread *_pThread = GetThread();     \
            Thread::ObjectRefFlush(_pThread);    \
        }                                       \
        FCallCheck __fCallCheck(__FILE__, __LINE__); \
        FCALL_TRANSITION_BEGIN(); \
        ::SetLastError(__lastError);            \

void FCallAssert(void*& cache, void* target);
void HCallAssert(void*& cache, void* target);

#else
#define FC_COMMON_PROLOG(target, assertFn) FCALL_TRANSITION_BEGIN()
#define FC_CAN_TRIGGER_GC()
#define FC_CAN_TRIGGER_GC_END()
#endif // ENABLE_CONTRACTS

// #FC_INNER
// Macros that allows fcall to be split into two function to avoid the helper frame overhead on common fast
// codepaths.
//
// The helper routine needs to know the name of the routine that called it so that it can look up the name of
// the managed routine this code is associted with (for managed stack traces). This is passed with the
// FC_INNER_PROLOG macro.
//
// The helper can set up a HELPER_METHOD_FRAME, but should pass the
// Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2 which indicates the exact number of
// unwinds to do to get back to managed code. Currently we only support depth 2 which means that the
// HELPER_METHOD_FRAME needs to be set up in the function directly called by the FCALL. The helper should
// use the NOINLINE macro to prevent the compiler from inlining it into the FCALL (which would obviously
// mess up the unwind count).
//
// The other invarient that needs to hold is that the epilog walker needs to be able to get from the call to
// the helper routine to the end of the FCALL using trivial heurisitics.   The easiest (and only supported)
// way of doing this is to place your helper right before a return (eg at the end of the method).  Generally
// this is not a problem at all, since the FCALL itself will pick off some common case and then tail-call to
// the helper for everything else.  You must use the code:FC_INNER_RETURN macros to do the call, to insure
// that the C++ compiler does not tail-call optimize the call to the inner function and mess up the stack
// depth.
//
// see code:ObjectNative::GetClass for an example
//
#define FC_INNER_PROLOG(outerfuncname)                          \
    LPVOID __me;                                                \
    __me = GetEEFuncEntryPointMacro(outerfuncname);             \
    FC_CAN_TRIGGER_GC();                                        \
    INCONTRACT(FCallCheck __fCallCheck(__FILE__, __LINE__));

// This variant should be used for inner fcall functions that have the
// __me value passed as an argument to the function. This allows
// inner functions to be shared across multiple fcalls.
#define FC_INNER_PROLOG_NO_ME_SETUP()                           \
    FC_CAN_TRIGGER_GC();                                        \
    INCONTRACT(FCallCheck __fCallCheck(__FILE__, __LINE__));

#define FC_INNER_EPILOG()                                       \
    FC_CAN_TRIGGER_GC_END();

// If you are using FC_INNER, and you are tail calling to the helper method (a common case), then you need
// to use the FC_INNER_RETURN macros (there is one for methods that return a value and another if the
// function returns void).  This macro's purpose is to inhibit any tail calll optimization the C++ compiler
// might do, which would otherwise confuse the epilog walker.
//
// * See #FC_INNER for more
extern RAW_KEYWORD(volatile) int FC_NO_TAILCALL;
#define FC_INNER_RETURN(type, expr)                                                        \
    type __retVal = expr;                                                                  \
    while (0 == FC_NO_TAILCALL) { }; /* side effect the compile can't remove */            \
    return(__retVal);

#define FC_INNER_RETURN_VOID(stmt)                                                         \
    stmt;                                                                                  \
    while (0 == FC_NO_TAILCALL) { }; /* side effect the compile can't remove */            \
    return;

//==============================================================================================
// FIMPLn: A set of macros for generating the proto for the actual
// implementation (use FDECLN for header protos.)
//
// The hidden "__me" variable lets us recover the original MethodDesc*
// so any thrown exceptions will have the correct stack trace. FCThrow()
// passes this along to __FCThrowInternal().
//==============================================================================================

#define GetEEFuncEntryPointMacro(func)  ((LPVOID)(func))

#define FCIMPL_PROLOG(funcname)  \
    LPVOID __me; \
    __me = GetEEFuncEntryPointMacro(funcname); \
    FC_COMMON_PROLOG(__me, FCallAssert)


#if defined(_DEBUG) && !defined(__GNUC__)
// Build the list of all fcalls signatures. It is used in binder.cpp to verify
// compatibility of managed and unmanaged fcall signatures. The check is currently done
// for x86 only.
#define CHECK_FCALL_SIGNATURE
#endif

#ifdef CHECK_FCALL_SIGNATURE
struct FCSigCheck {
public:
    FCSigCheck(void* fnc, const char* sig)
    {
        LIMITED_METHOD_CONTRACT;
        func = fnc;
        signature = sig;
        next = g_pFCSigCheck;
        g_pFCSigCheck = this;
    }

    FCSigCheck* next;
    void* func;
    const char* signature;

    static FCSigCheck* g_pFCSigCheck;
};

#define FCSIGCHECK(funcname, signature) \
    static FCSigCheck UNIQUE_LABEL(FCSigCheck)(GetEEFuncEntryPointMacro(funcname), signature);

#else // CHECK_FCALL_SIGNATURE

#define FCSIGCHECK(funcname, signature)

#endif // !CHECK_FCALL_SIGNATURE


#ifdef SWIZZLE_STKARG_ORDER
#ifdef SWIZZLE_REGARG_ORDER

#define FCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname() { FCIMPL_PROLOG(funcname)
#define FCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a2, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL2_VI(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a2, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_IIV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_VII(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a3, a2, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1, a3, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a3, a1, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a3, a2, a1) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a3, a2, a1) {  FCIMPL_PROLOG(funcname)
#define FCIMPL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL6(rettype, funcname, a1, a2, a3, a4, a5, a6) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL9(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL10(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL11(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL12(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL13(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL14(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a14, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)

#define FCIMPL5_IVI(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(int /* EAX */, a3, a1, a5, a4, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL5_VII(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(int /* EAX */, a3, a2, a5, a4, a1) { FCIMPL_PROLOG(funcname)

#else // SWIZZLE_REGARG_ORDER

#define FCIMPL0(rettype, funcname) FCSIGCHECK(funcname, #rettype) \
    rettype F_CALL_CONV funcname() { FCIMPL_PROLOG(funcname)
#define FCIMPL1(rettype, funcname, a1) FCSIGCHECK(funcname, #rettype "," #a1) \
    rettype F_CALL_CONV funcname(a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL1_V(rettype, funcname, a1) FCSIGCHECK(funcname, #rettype "," "V" #a1) \
    rettype F_CALL_CONV funcname(a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL2(rettype, funcname, a1, a2) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2) \
    rettype F_CALL_CONV funcname(a1, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL2VA(rettype, funcname, a1, a2) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," "...") \
    rettype F_CALL_VA_CONV funcname(a1, a2, ...) { FCIMPL_PROLOG(funcname)
#define FCIMPL2_VV(rettype, funcname, a1, a2) FCSIGCHECK(funcname, #rettype "," "V" #a1 "," "V" #a2) \
    rettype F_CALL_CONV funcname(a2, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL2_VI(rettype, funcname, a1, a2) FCSIGCHECK(funcname, #rettype "," "V" #a1 "," #a2) \
    rettype F_CALL_CONV funcname(a2, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL2_IV(rettype, funcname, a1, a2) FCSIGCHECK(funcname, #rettype "," #a1 "," "V" #a2) \
    rettype F_CALL_CONV funcname(a1, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL3(rettype, funcname, a1, a2, a3) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3) \
    rettype F_CALL_CONV funcname(a1, a2, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_IIV(rettype, funcname, a1, a2, a3) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," "V" #a3) \
    rettype F_CALL_CONV funcname(a1, a2, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_VII(rettype, funcname, a1, a2, a3) FCSIGCHECK(funcname, #rettype "," "V" #a1 "," #a2 "," #a3) \
    rettype F_CALL_CONV funcname(a2, a3, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVV(rettype, funcname, a1, a2, a3) FCSIGCHECK(funcname, #rettype "," #a1 "," "V" #a2 "," "V" #a3) \
    rettype F_CALL_CONV funcname(a1, a3, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVI(rettype, funcname, a1, a2, a3) FCSIGCHECK(funcname, #rettype "," #a1 "," "V" #a2 "," #a3) \
    rettype F_CALL_CONV funcname(a1, a3, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVI(rettype, funcname, a1, a2, a3) FCSIGCHECK(funcname, #rettype "," "V" #a1 "," "V" #a2 "," #a3) \
    rettype F_CALL_CONV funcname(a2, a1, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVV(rettype, funcname, a1, a2, a3) FCSIGCHECK(funcname, #rettype "," "V" #a1 "," "V" #a2 "," "V" #a3) \
    rettype F_CALL_CONV funcname(a3, a2, a1) { FCIMPL_PROLOG(funcname)
#define FCIMPL4(rettype, funcname, a1, a2, a3, a4) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4) \
    rettype F_CALL_CONV funcname(a1, a2, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5) \
    rettype F_CALL_CONV funcname(a1, a2, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL6(rettype, funcname, a1, a2, a3, a4, a5, a6) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6) \
    rettype F_CALL_CONV funcname(a1, a2, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7) \
    rettype F_CALL_CONV funcname(a1, a2, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7 "," #a8) \
    rettype F_CALL_CONV funcname(a1, a2, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL9(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7 "," #a8 "," #a9) \
    rettype F_CALL_CONV funcname(a1, a2, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL10(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7 "," #a8 "," #a9 "," #a10) \
    rettype F_CALL_CONV funcname(a1, a2, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL11(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7 "," #a8 "," #a9 "," #a10 "," #a11) \
    rettype F_CALL_CONV funcname(a1, a2, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL12(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7 "," #a8 "," #a9 "," #a10 "," #a11 "," #a12) \
    rettype F_CALL_CONV funcname(a1, a2, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL13(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7 "," #a8 "," #a9 "," #a10 "," #a11 "," #a12 "," #a13) \
    rettype F_CALL_CONV funcname(a1, a2, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)
#define FCIMPL14(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) FCSIGCHECK(funcname, #rettype "," #a1 "," #a2 "," #a3 "," #a4 "," #a5 "," #a6 "," #a7 "," #a8 "," #a9 "," #a10 "," #a11 "," #a12 "," #a13 "," #a14) \
    rettype F_CALL_CONV funcname(a1, a2, a14, a13, a12, a11, a10, a9, a8, a7, a6, a5, a4, a3) { FCIMPL_PROLOG(funcname)

#define FCIMPL5_IVI(rettype, funcname, a1, a2, a3, a4, a5) FCSIGCHECK(funcname, #rettype "," #a1 "," "V" #a2 "," #a3 "," #a4 "," #a5) \
    rettype F_CALL_CONV funcname(a1, a3, a5, a4, a2) { FCIMPL_PROLOG(funcname)
#define FCIMPL5_VII(rettype, funcname, a1, a2, a3, a4, a5) FCSIGCHECK(funcname, #rettype "," "V" #a1 "," #a2 "," #a3 "," #a4 "," #a5) \
    rettype F_CALL_CONV funcname(a2, a3, a5, a4, a1) { FCIMPL_PROLOG(funcname)

#endif // !SWIZZLE_REGARG_ORDER

#else // SWIZZLE_STKARG_ORDER

#define FCIMPL0(rettype, funcname) rettype funcname() { FCIMPL_PROLOG(funcname)
#define FCIMPL1(rettype, funcname, a1) rettype funcname(a1) {  FCIMPL_PROLOG(funcname)
#define FCIMPL1_V(rettype, funcname, a1) rettype funcname(a1) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2(rettype, funcname, a1, a2) rettype funcname(a1, a2) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2VA(rettype, funcname, a1, a2) rettype funcname(a1, a2, ...) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2_VV(rettype, funcname, a1, a2) rettype funcname(a1, a2) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2_VI(rettype, funcname, a1, a2) rettype funcname(a1, a2) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2_IV(rettype, funcname, a1, a2) rettype funcname(a1, a2) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3(rettype, funcname, a1, a2, a3) rettype funcname(a1, a2, a3) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_IIV(rettype, funcname, a1, a2, a3) rettype funcname(a1, a2, a3) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVV(rettype, funcname, a1, a2, a3) rettype funcname(a1, a2, a3) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_VII(rettype, funcname, a1, a2, a3) rettype funcname(a1, a2, a3) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVI(rettype, funcname, a1, a2, a3) rettype funcname(a1, a2, a3) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVI(rettype, funcname, a1, a2, a3) rettype funcname(a1, a2, a3) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVV(rettype, funcname, a1, a2, a3) rettype funcname(a1, a2, a3) {  FCIMPL_PROLOG(funcname)
#define FCIMPL4(rettype, funcname, a1, a2, a3, a4) rettype funcname(a1, a2, a3, a4) {  FCIMPL_PROLOG(funcname)
#define FCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) rettype funcname(a1, a2, a3, a4, a5) {  FCIMPL_PROLOG(funcname)
#define FCIMPL6(rettype, funcname, a1, a2, a3, a4, a5, a6) rettype funcname(a1, a2, a3, a4, a5, a6) {  FCIMPL_PROLOG(funcname)
#define FCIMPL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) rettype funcname(a1, a2, a3, a4, a5, a6, a7) {  FCIMPL_PROLOG(funcname)
#define FCIMPL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) rettype funcname(a1, a2, a3, a4, a5, a6, a7, a8) {  FCIMPL_PROLOG(funcname)
#define FCIMPL9(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9) rettype funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9) {  FCIMPL_PROLOG(funcname)
#define FCIMPL10(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) rettype funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) {  FCIMPL_PROLOG(funcname)
#define FCIMPL11(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) rettype funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) {  FCIMPL_PROLOG(funcname)
#define FCIMPL12(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) rettype funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) {  FCIMPL_PROLOG(funcname)
#define FCIMPL13(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) rettype funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) {  FCIMPL_PROLOG(funcname)
#define FCIMPL14(rettype,funcname, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) rettype funcname(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) {  FCIMPL_PROLOG(funcname)

#define FCIMPL5_IVI(rettype, funcname, a1, a2, a3, a4, a5) rettype funcname(a1, a2, a3, a4, a5) { FCIMPL_PROLOG(funcname)
#define FCIMPL5_VII(rettype, funcname, a1, a2, a3, a4, a5) rettype funcname(a1, a2, a3, a4, a5) { FCIMPL_PROLOG(funcname)

#endif // !SWIZZLE_STKARG_ORDER

//==============================================================================================
// Use this to terminte an FCIMPLEND.
//==============================================================================================

#define FCIMPL_EPILOG()   FCALL_TRANSITION_END()

#define FCIMPLEND   FCIMPL_EPILOG(); }

#define HCIMPL_PROLOG(funcname) LPVOID __me; __me = 0; FC_COMMON_PROLOG(funcname, HCallAssert)

    // HCIMPL macros are just like their FCIMPL counterparts, however
    // they do not remember the function they come from. Thus they will not
    // show up in a stack trace.  This is what you want for JIT helpers and the like

#ifdef SWIZZLE_STKARG_ORDER
#ifdef SWIZZLE_REGARG_ORDER

#define HCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname() { HCIMPL_PROLOG(funcname)
#define HCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL1_RAW(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1) {
#define HCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_RAW(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1) {
#define HCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a2, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2VA(rettype, funcname, a1, a2) rettype F_CALL_VA_CONV funcname(a1, a2, ...) { HCIMPL_PROLOG(funcname)
#define HCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL3_RAW(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3) {
#define HCIMPL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a4, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a5, a4, a3) { HCIMPL_PROLOG(funcname)

#define HCCALL0(funcname)               funcname()
#define HCCALL1(funcname, a1)           funcname(0, 0, a1)
#define HCCALL1_V(funcname, a1)         funcname(0, 0, 0, a1)
#define HCCALL2(funcname, a1, a2)       funcname(0, a2, a1)
#define HCCALL3(funcname, a1, a2, a3)   funcname(0, a2, a1, a3)
#define HCCALL4(funcname, a1, a2, a3, a4)       funcname(0, a2, a1, a4, a3)
#define HCCALL5(funcname, a1, a2, a3, a4, a5)   funcname(0, a2, a1, a5, a4, a3)
#define HCCALL1_PTR(rettype, funcptr, a1)        rettype (F_CALL_CONV * funcptr)(int /* EAX */, int /* EDX */, a1)
#define HCCALL2_PTR(rettype, funcptr, a1, a2)    rettype (F_CALL_CONV * funcptr)(int /* EAX */, a2, a1)
#else // SWIZZLE_REGARG_ORDER

#define HCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname() { HCIMPL_PROLOG(funcname)
#define HCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL1_RAW(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) {
#define HCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_RAW(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) {
#define HCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a2, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2VA(rettype, funcname, a1, a2) rettype F_CALL_VA_CONV funcname(a1, a2, ...) { HCIMPL_PROLOG(funcname)
#define HCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL3_RAW(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) {
#define HCIMPL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(a1, a2, a4, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a1, a2, a5, a4, a3) { HCIMPL_PROLOG(funcname)

#define HCCALL0(funcname)               funcname()
#define HCCALL1(funcname, a1)           funcname(a1)
#define HCCALL1_V(funcname, a1)         funcname(a1)
#define HCCALL2(funcname, a1, a2)       funcname(a1, a2)
#define HCCALL3(funcname, a1, a2, a3)   funcname(a1, a2, a3)
#define HCCALL4(funcname, a1, a2, a3, a4)       funcname(a1, a2, a4, a3)
#define HCCALL5(funcname, a1, a2, a3, a4, a5)   funcname(a1, a2, a5, a4, a3)
#define HCCALL1_PTR(rettype, funcptr, a1)        rettype (F_CALL_CONV * (funcptr))(a1)
#define HCCALL2_PTR(rettype, funcptr, a1, a2)    rettype (F_CALL_CONV * (funcptr))(a1, a2)
#endif // !SWIZZLE_REGARG_ORDER
#else // SWIZZLE_STKARG_ORDER

#define HCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname() { HCIMPL_PROLOG(funcname)
#define HCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL1_RAW(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) {
#define HCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_RAW(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) {
#define HCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2VA(rettype, funcname, a1, a2) rettype F_CALL_VA_CONV funcname(a1, a2, ...) { HCIMPL_PROLOG(funcname)
#define HCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL3_RAW(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) {
#define HCIMPL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(a1, a2, a3, a4) { HCIMPL_PROLOG(funcname)
#define HCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(a1, a2, a3, a4, a5) { HCIMPL_PROLOG(funcname)

#define HCCALL0(funcname)               funcname()
#define HCCALL1(funcname, a1)           funcname(a1)
#define HCCALL1_V(funcname, a1)         funcname(a1)
#define HCCALL2(funcname, a1, a2)       funcname(a1, a2)
#define HCCALL3(funcname, a1, a2, a3)   funcname(a1, a2, a3)
#define HCCALL4(funcname, a1, a2, a3, a4)       funcname(a1, a2, a3, a4)
#define HCCALL5(funcname, a1, a2, a3, a4, a5)   funcname(a1, a2, a3, a4, a5)
#define HCCALL1_PTR(rettype, funcptr, a1)        rettype (F_CALL_CONV * (funcptr))(a1)
#define HCCALL2_PTR(rettype, funcptr, a1, a2)    rettype (F_CALL_CONV * (funcptr))(a1, a2)

#endif // !SWIZZLE_STKARG_ORDER

#define HCIMPLEND_RAW   }
#define HCIMPLEND       FCALL_TRANSITION_END(); }


//==============================================================================================
// Throws an exception from an FCall. See rexcep.h for a list of valid
// exception codes.
//==============================================================================================
#define FCThrow(reKind) FCThrowEx(reKind, 0, 0, 0, 0)

//==============================================================================================
// This version lets you attach a message with inserts (similar to
// COMPlusThrow()).
//==============================================================================================
#define FCThrowEx(reKind, resID, arg1, arg2, arg3)              \
    {                                                           \
        while (NULL ==                                          \
            __FCThrow(__me, reKind, resID, arg1, arg2, arg3)) {}; \
        return 0;                                               \
    }

//==============================================================================================
// Like FCThrow but can be used for a VOID-returning FCall. The only
// difference is in the "return" statement.
//==============================================================================================
#define FCThrowVoid(reKind) FCThrowExVoid(reKind, 0, 0, 0, 0)

//==============================================================================================
// This version lets you attach a message with inserts (similar to
// COMPlusThrow()).
//==============================================================================================
#define FCThrowExVoid(reKind, resID, arg1, arg2, arg3)          \
    {                                                           \
        while (NULL ==                                          \
            __FCThrow(__me, reKind, resID, arg1, arg2, arg3)) {}; \
        return;                                                 \
    }

// Use FCThrowRes to throw an exception with a localized error message from the
// ResourceManager in managed code.
#define FCThrowRes(reKind, resourceName) FCThrowArgumentEx(reKind, NULL, resourceName)
#define FCThrowArgumentNull(argName) FCThrowArgumentEx(kArgumentNullException, argName, NULL)
#define FCThrowArgumentOutOfRange(argName, message) FCThrowArgumentEx(kArgumentOutOfRangeException, argName, message)
#define FCThrowArgument(argName, message) FCThrowArgumentEx(kArgumentException, argName, message)

#define FCThrowArgumentEx(reKind, argName, resourceName)        \
    {                                                           \
        while (NULL ==                                                  \
            __FCThrowArgument(__me, reKind, argName, resourceName)) {}; \
        return 0;                                               \
    }

// Use FCThrowRes to throw an exception with a localized error message from the
// ResourceManager in managed code.
#define FCThrowResVoid(reKind, resourceName) FCThrowArgumentVoidEx(reKind, NULL, resourceName)
#define FCThrowArgumentNullVoid(argName) FCThrowArgumentVoidEx(kArgumentNullException, argName, NULL)
#define FCThrowArgumentOutOfRangeVoid(argName, message) FCThrowArgumentVoidEx(kArgumentOutOfRangeException, argName, message)
#define FCThrowArgumentVoid(argName, message) FCThrowArgumentVoidEx(kArgumentException, argName, message)

#define FCThrowArgumentVoidEx(reKind, argName, resourceName)    \
    {                                                           \
        while (NULL ==                                                  \
            __FCThrowArgument(__me, reKind, argName, resourceName)) {};  \
        return;                                                 \
    }



// The x86 JIT calling convention expects returned small types (e.g. bool) to be
// widened on return. The C/C++ calling convention does not guarantee returned
// small types to be widened. The small types has to be artifically widened on return
// to fit x86 JIT calling convention. Thus fcalls returning small types has to
// use the FC_XXX_RET types to force C/C++ compiler to do the widening.
//
// The most common small return type of FCALLs is bool. The widening of bool is
// especially tricky since the value has to be also normalized. FC_BOOL_RET and
// FC_RETURN_BOOL macros are provided to make it fool-proof. FCALLs returning bool
// should be implemented using following pattern:

// FCIMPL0(FC_BOOL_RET, Foo)    // the return type should be FC_BOOL_RET
//      BOOL ret;
//
//      FC_RETURN_BOOL(ret);    // return statements should be FC_RETURN_BOOL
// FCIMPLEND

// This rules are verified in binder.cpp if COMPlus_ConsistencyCheck is set.

#ifdef _PREFAST_

// Use prefast build to ensure that functions returning FC_BOOL_RET
// are using FC_RETURN_BOOL to return it. Missing FC_RETURN_BOOL will
// result into type mismatch error in prefast builds. This will also
// catch misuses of FC_BOOL_RET for other places (e.g. in FCALL parameters).

typedef LPVOID FC_BOOL_RET;
#define FC_RETURN_BOOL(x) do { return (LPVOID)!!(x); } while(0)

#else

#if defined(TARGET_X86) || defined(TARGET_AMD64)
// The return value is artifically widened on x86 and amd64
typedef INT32 FC_BOOL_RET;
#else
typedef CLR_BOOL FC_BOOL_RET;
#endif

#define FC_RETURN_BOOL(x)   do { return !!(x); } while(0)

#endif


#if defined(TARGET_X86) || defined(TARGET_AMD64)
// The return value is artifically widened on x86 and amd64
typedef UINT32 FC_CHAR_RET;
typedef INT32 FC_INT8_RET;
typedef UINT32 FC_UINT8_RET;
typedef INT32 FC_INT16_RET;
typedef UINT32 FC_UINT16_RET;
#else
typedef CLR_CHAR FC_CHAR_RET;
typedef INT8 FC_INT8_RET;
typedef UINT8 FC_UINT8_RET;
typedef INT16 FC_INT16_RET;
typedef UINT16 FC_UINT16_RET;
#endif


// FC_TypedByRef should be used for TypedReferences in FCall signatures
#define FC_TypedByRef   TypedByRef
#define FC_DECIMAL      DECIMAL


// The fcall entrypoints has to be at unique addresses. Use this helper macro to make
// the code of the fcalls unique if you get assert in ecall.cpp that mentions it.
// The parameter of the FCUnique macro is an arbitrary 32-bit random non-zero number.
#define FCUnique(unique) { Volatile<int> u = (unique); while (u.LoadWithoutBarrier() == 0) { }; }




// FCALL contracts come in two forms:
//
// Short form that should be used if the FCALL contract does not have any extras like preconditions, failure injection. Example:
//
// FCIMPL0(void, foo)
// {
//     FCALL_CONTRACT;
//     ...
//
// Long form that should be used otherwise. Example:
//
// FCIMPL1(void, foo, void *p)
// {
//     CONTRACTL {
//         FCALL_CHECK;
//         PRECONDITION(CheckPointer(p));
//     } CONTRACTL_END;
//     ...


//
// FCALL_CHECK defines the actual contract conditions required for FCALLs
//
#define FCALL_CHECK \
        THROWS; \
        DISABLED(GC_TRIGGERS); /* FCALLS with HELPER frames have issues with GC_TRIGGERS */ \
        MODE_COOPERATIVE;

//
// FCALL_CONTRACT should be the following shortcut:
//
// #define FCALL_CONTRACT   CONTRACTL { FCALL_CHECK; } CONTRACTL_END;
//
// Since there is very little value in having runtime contracts in FCalls, FCALL_CONTRACT is defined as static contract only for performance reasons.
//
#define FCALL_CONTRACT \
    STATIC_CONTRACT_THROWS; \
    /* FCALLS are a special case contract wise, they are "NOTRIGGER, unless you setup a frame" */ \
    STATIC_CONTRACT_GC_NOTRIGGER; \
    STATIC_CONTRACT_MODE_COOPERATIVE

#endif //__FCall_h__
