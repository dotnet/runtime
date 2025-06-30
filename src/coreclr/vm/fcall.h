// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// FCall is a high-performance call into unmanaged runtime code from managed code.
// The managed code calls the FCall entry point directly.

// Causing GC or EH in an FCALL is illegal. QCalls should be used instead.

// Compile time errors occur if you try to violate either of these rules.

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
//  - On x86, if first and/or second argument of your FCall cannot be passed
//    in either of the __fastcall registers (ECX/EDX), you must use "V" versions
//    of FCDECL and  FCIMPL macros to enregister arguments correctly. Some of the
//    most common types that fit this requirement are 64-bit values (i.e. INT64 or
//    UINT64) and floating-point values (i.e. FLOAT or DOUBLE). For example, FCDECL3_IVI
//    must be used for FCalls that take 3 arguments and 2nd argument is INT64 and
//    FDECL2_VV must be used for FCalls that take 2 arguments where both are FLOAT.

// How FCall works:
// ----------------
//   An FCall target uses __fastcall or some other calling convention to
//   match the IL calling convention exactly. Thus, a call to FCall is a direct
//   call to the target w/ no intervening stub or frame.

#ifndef __FCall_h__
#define __FCall_h__

#include "runtimeexceptionkind.h"

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
        ForbidGC __fCallCheck(__FILE__, __LINE__); \
        ::SetLastError(__lastError);            \

void FCallAssert(void*& cache, void* target);
void HCallAssert(void*& cache, void* target);

#else
#define FC_COMMON_PROLOG(target, assertFn)
#define FC_CAN_TRIGGER_GC()
#define FC_CAN_TRIGGER_GC_END()
#endif // ENABLE_CONTRACTS

//==============================================================================================
// FIMPLn: A set of macros for generating the proto for the actual
// implementation (use FDECLN for header protos.)
//
// The hidden "__me" variable lets us recover the original MethodDesc*
// so any thrown exceptions will have the correct stack trace.
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

#define FCIMPLEND }

#define HCIMPL_PROLOG(funcname) LPVOID __me; __me = 0; FC_COMMON_PROLOG(funcname, HCallAssert)

// HCIMPL macros are used to implement JIT helpers. The only difference is that
// HCIMPL methods are not mapped to a managed method in CoreLib in ecalllist.h.

#ifdef SWIZZLE_STKARG_ORDER
#ifdef SWIZZLE_REGARG_ORDER

#define HCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname() { HCIMPL_PROLOG(funcname)
#define HCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL1_RAW(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, a1) {
#define HCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_RAW(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1) {
#define HCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(int /* EAX */, int /* EDX */, int /* ECX */, a2, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL3_RAW(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(int /* EAX */, a2, a1, a3) {
#else // SWIZZLE_REGARG_ORDER

#define HCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname() { HCIMPL_PROLOG(funcname)
#define HCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL1_RAW(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) {
#define HCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_RAW(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) {
#define HCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a2, a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL3_RAW(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) {
#endif // !SWIZZLE_REGARG_ORDER
#else // SWIZZLE_STKARG_ORDER

#define HCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname() { HCIMPL_PROLOG(funcname)
#define HCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL1_RAW(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) {
#define HCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1) { HCIMPL_PROLOG(funcname)
#define HCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_RAW(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) {
#define HCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2) { HCIMPL_PROLOG(funcname)
#define HCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) { HCIMPL_PROLOG(funcname)
#define HCIMPL3_RAW(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(a1, a2, a3) {
#endif // !SWIZZLE_STKARG_ORDER

#define HCIMPLEND_RAW }
#define HCIMPLEND }

// The managed calling convention expects returned small types (e.g. bool) to be
// widened to 32-bit on return. The C/C++ calling convention does not guarantee returned
// small types to be widened on most platforms. The small types have to be artificially
// widened on return to fit the managed calling convention. Thus fcalls returning small
// types have to use the FC_XXX_RET types to force C/C++ compiler to do the widening.
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

// This rule is verified in corelib.cpp if DOTNET_ConsistencyCheck is set.

// The return value is artificially widened in managed calling convention
typedef INT32 FC_BOOL_RET;

#define FC_RETURN_BOOL(x)   do { return !!(x); } while(0)

// Small primitive return values are artificially widened in managed calling convention
typedef UINT32 FC_CHAR_RET;
typedef INT32 FC_INT8_RET;
typedef UINT32 FC_UINT8_RET;
typedef INT32 FC_INT16_RET;
typedef UINT32 FC_UINT16_RET;

// Small primitive args are not widened.
typedef INT32 FC_BOOL_ARG;

#define FC_ACCESS_BOOL(x) ((BYTE)x != 0)

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
#define FCALL_CONTRACT \
    STATIC_CONTRACT_THROWS; \
    /* FCALLS are a special case contract wise, they are "NOTRIGGER, unless you setup a frame" */ \
    STATIC_CONTRACT_GC_NOTRIGGER; \
    STATIC_CONTRACT_MODE_COOPERATIVE

#endif //__FCall_h__
