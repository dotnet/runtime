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


#else // SWIZZLE_REGARG_ORDER

#define FCDECL0(rettype, funcname) rettype F_CALL_CONV funcname()
#define FCDECL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
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


#endif // !SWIZZLE_REGARG_ORDER

#elif defined(TARGET_WASM)

#define FCDECL0(rettype, funcname) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, PCODE portableEntryPointContext)
#define FCDECL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, PCODE portableEntryPointContext)
#define FCDECL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, PCODE portableEntryPointContext)
#define FCDECL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext)
#define FCDECL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext)
#define FCDECL2_VI(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext)
#define FCDECL2_IV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext)
#define FCDECL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext)
#define FCDECL3_IIV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext)
#define FCDECL3_VII(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext)
#define FCDECL3_IVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext)
#define FCDECL3_IVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext)
#define FCDECL3_VVI(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext)
#define FCDECL3_VVV(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext)
#define FCDECL4(rettype, funcname, a1, a2, a3, a4) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, a4, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, a4, PCODE portableEntryPointContext)
#define FCDECL5(rettype, funcname, a1, a2, a3, a4, a5) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, PCODE portableEntryPointContext)
#define FCDECL6(rettype, funcname, a1, a2, a3, a4, a5, a6) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, a6, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, a6, PCODE portableEntryPointContext)
#define FCDECL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, a6, a7, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, a6, a7, PCODE portableEntryPointContext)
#define FCDECL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, a6, a7, a8, PCODE portableEntryPointContext); rettype F_CALL_CONV funcname ## _IMPL(uintptr_t callersStackPointer, a1, a2, a3, a4, a5, a6, a7, a8, PCODE portableEntryPointContext)


#else // !SWIZZLE_STKARG_ORDER

#define FCDECL0(rettype, funcname) rettype F_CALL_CONV funcname()
#define FCDECL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(a1)
#define FCDECL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(a1, a2)
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
#define FC_COMMON_PROLOG()                      \
        /* The following line has to be first.  We do not want to trash last error */ \
        DWORD __lastError = ::GetLastError();   \
        ForbidGC __fCallCheck(__FILE__, __LINE__); \
        ::SetLastError(__lastError);            \

#else
#define FC_COMMON_PROLOG()
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
    FC_COMMON_PROLOG()


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

#endif // !SWIZZLE_REGARG_ORDER

#elif defined(TARGET_WASM)

#define WASM_WRAPPER_FUNC_INITIAL \
{ \
    asm ("local.get 0\n" /* Capture callersStackPointer onto the stack for calling _IMPL function*/ \
         "local.get 0\n" /* Capture callersStackPointer onto the stack for setting the __stack_pointer */ \
         "global.get __stack_pointer\n" /* Get current value of stack global */ \
         "local.set 0\n"  /* Store current stack global into callersStackPointer local */ \
         "global.set __stack_pointer\n" /* Set stack global to the initial value of callersStackPointer, which is the current stack pointer for the interpreter call */

#define WASM_WRAPPER_FUNC_EPILOG(_method) \
             "call %0\n" /* Call the actual implementation function*/ \
             "local.get 0\n" /* Load the original stack pointer from stack Arg local */ \
             "global.set __stack_pointer\n" /* Restore the original stack pointer to the stack global */ \
             "return" :: "i" (_method ## _IMPL)); \
}

#define WASM_WRAPPER_FUNC_0(_rettype, _method) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer) WASM_WRAPPER_FUNC_INITIAL WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_1(_rettype, _method, a) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a) WASM_WRAPPER_FUNC_INITIAL "local.get 1\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_2(_rettype, _method, a, b) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_3(_rettype, _method, a, b, c) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_4(_rettype, _method, a, b, c, d) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_5(_rettype, _method, a, b, c, d, e) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d, e) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\nlocal.get 5\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_6(_rettype, _method, a, b, c, d, e, f) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d, e, f) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\nlocal.get 5\nlocal.get 6\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_7(_rettype, _method, a, b, c, d, e, f, g) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d, e, f, g) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\nlocal.get 5\nlocal.get 6\nlocal.get 7\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_8(_rettype, _method, a, b, c, d, e, f, g, h) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d, e, f, g, h) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\nlocal.get 5\nlocal.get 6\nlocal.get 7\nlocal.get 8\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_9(_rettype, _method, a, b, c, d, e, f, g, h, i) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d, e, f, g, h, i) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\nlocal.get 5\nlocal.get 6\nlocal.get 7\nlocal.get 8\nlocal.get 9\n" WASM_WRAPPER_FUNC_EPILOG(_method)

#define WASM_CALLABLE_FUNC_0(_rettype, _method) \
    WASM_WRAPPER_FUNC_0(_rettype, _method) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer)

#define WASM_CALLABLE_FUNC_1(_rettype, _method, a) \
    WASM_WRAPPER_FUNC_1(_rettype, _method, a) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a)

#define WASM_CALLABLE_FUNC_2(_rettype, _method, a, b) \
    WASM_WRAPPER_FUNC_2(_rettype, _method, a, b) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b)

#define WASM_CALLABLE_FUNC_3(_rettype, _method, a, b, c) \
    WASM_WRAPPER_FUNC_3(_rettype, _method, a, b, c) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c)

#define WASM_CALLABLE_FUNC_4(_rettype, _method, a, b, c, d) \
    WASM_WRAPPER_FUNC_4(_rettype, _method, a, b, c, d) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d)

#define WASM_CALLABLE_FUNC_5(_rettype, _method, a, b, c, d, e) \
    WASM_WRAPPER_FUNC_5(_rettype, _method, a, b, c, d, e) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d, e)

#define WASM_CALLABLE_FUNC_6(_rettype, _method, a, b, c, d, e, f) \
    WASM_WRAPPER_FUNC_6(_rettype, _method, a, b, c, d, e, f) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d, e, f)

#define WASM_CALLABLE_FUNC_7(_rettype, _method, a, b, c, d, e, f, g) \
    WASM_WRAPPER_FUNC_7(_rettype, _method, a, b, c, d, e, f, g) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d, e, f, g)

#define WASM_CALLABLE_FUNC_8(_rettype, _method, a, b, c, d, e, f, g, h) \
    WASM_WRAPPER_FUNC_8(_rettype, _method, a, b, c, d, e, f, g, h) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d, e, f, g, h)

#define WASM_CALLABLE_FUNC_9(_rettype, _method, a, b, c, d, e, f, g, h, i) \
    WASM_WRAPPER_FUNC_9(_rettype, _method, a, b, c, d, e, f, g, h, i) \
    _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d, e, f, g, h, i)


#define FCIMPL0(rettype, funcname) WASM_CALLABLE_FUNC_1(rettype, funcname, PCODE portableEntryPointContext) { FCIMPL_PROLOG(funcname)
#define FCIMPL1(rettype, funcname, a1) WASM_CALLABLE_FUNC_2(rettype, funcname, a1, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL1_V(rettype, funcname, a1) WASM_CALLABLE_FUNC_2(rettype, funcname, a1, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2(rettype, funcname, a1, a2) WASM_CALLABLE_FUNC_3(rettype, funcname, a1, a2, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2VA(rettype, funcname, a1, a2) WASM_CALLABLE_FUNC_3(rettype, funcname, a1, a2, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2_VV(rettype, funcname, a1, a2) WASM_CALLABLE_FUNC_3(rettype, funcname, a1, a2, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2_VI(rettype, funcname, a1, a2) WASM_CALLABLE_FUNC_3(rettype, funcname, a1, a2, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL2_IV(rettype, funcname, a1, a2) WASM_CALLABLE_FUNC_3(rettype, funcname, a1, a2, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_IIV(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVV(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_VII(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_IVI(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVI(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL3_VVV(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL4(rettype, funcname, a1, a2, a3, a4) WASM_CALLABLE_FUNC_5(rettype, funcname, a1, a2, a3, a4, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) WASM_CALLABLE_FUNC_6(rettype, funcname, a1, a2, a3, a4, a5, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL6(rettype, funcname, a1, a2, a3, a4, a5, a6) WASM_CALLABLE_FUNC_7(rettype, funcname, a1, a2, a3, a4, a5, a6, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL7(rettype, funcname, a1, a2, a3, a4, a5, a6, a7) WASM_CALLABLE_FUNC_8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)
#define FCIMPL8(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8) WASM_CALLABLE_FUNC_9(rettype, funcname, a1, a2, a3, a4, a5, a6, a7, a8, PCODE portableEntryPointContext) {  FCIMPL_PROLOG(funcname)

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

#endif // !SWIZZLE_STKARG_ORDER

//==============================================================================================
// Use this to terminte an FCIMPLEND.
//==============================================================================================

#define FCIMPLEND }

#define HCIMPL_PROLOG(funcname) FC_COMMON_PROLOG()

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
#elif defined(TARGET_WASM)

#define HCIMPL0(rettype, funcname) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, PCODE portableEntryPointContext) { HCIMPL_PROLOG(funcname)
#define HCIMPL1(rettype, funcname, a1) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, PCODE portableEntryPointContext) { HCIMPL_PROLOG(funcname)
#define HCIMPL1_RAW(rettype, funcname, a1) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, PCODE portableEntryPointContext) {
#define HCIMPL1_V(rettype, funcname, a1) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, PCODE portableEntryPointContext) { HCIMPL_PROLOG(funcname)
#define HCIMPL2(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext) { HCIMPL_PROLOG(funcname)
#define HCIMPL2_RAW(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext) {
#define HCIMPL2_VV(rettype, funcname, a1, a2) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, PCODE portableEntryPointContext) { HCIMPL_PROLOG(funcname)
#define HCIMPL3(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext) { HCIMPL_PROLOG(funcname)
#define HCIMPL3_RAW(rettype, funcname, a1, a2, a3) rettype F_CALL_CONV funcname(uintptr_t callersStackPointer, a1, a2, a3, PCODE portableEntryPointContext) {

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
