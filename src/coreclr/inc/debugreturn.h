// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _DEBUGRETURN_H_
#define _DEBUGRETURN_H_

// This is disabled in build 190024315 (a pre-release build after VS 2015 Update 3) and
// earlier because those builds only support C++11 constexpr,  which doesn't allow the
// use of 'if' statements within the body of a constexpr function.  Later builds support
// C++14 constexpr.
#if defined(_DEBUG) && !defined(JIT_BUILD) && (!defined(_MSC_FULL_VER) || _MSC_FULL_VER > 190024315)

// Code to generate a compile-time error if return statements appear where they
// shouldn't.
//
// Here's the way it works...
//
// We create two classes with a safe_to_return() method.  The method is static,
// returns void, and does nothing.  One class has the method as public, the other
// as private.  We introduce a global scope typedef for __ReturnOK that refers to
// the class with the public method.  So, by default, the expression
//
//      __ReturnOK::safe_to_return()
//
// quietly compiles and does nothing.  When we enter a block in which we want to
// inhibit returns, we introduce a new typedef that defines __ReturnOK as the
// class with the private method.  Inside this scope,
//
//      __ReturnOK::safe_to_return()
//
// generates a compile-time error.
//
// To cause the method to be called, we have to #define the return keyword.
// The simplest working version would be
//
//   #define return if (0) __ReturnOK::safe_to_return(); else return
//
// but we've used
//
//   #define return for (;1;__ReturnOK::safe_to_return()) return
//
// because it happens to generate somewhat faster code in a checked build.  (They
// both introduce no overhead in a fastchecked build.)
//
class __SafeToReturn {
public:
    static int safe_to_return() {return 0;};
    static int used() {return 0;};
};

class __YouCannotUseAReturnStatementHere {
private:
    // If you got here, and you're wondering what you did wrong -- you're using
    // a return statement where it's not allowed.  Likely, it's inside one of:
    //     GCPROTECT_BEGIN ... GCPROTECT_END
    //
    static int safe_to_return() {return 0;};
public:
    // Some compilers warn if all member functions in a class are private
    // or if a typedef is unused. Rather than disable the warning, we'll work
    // around it here.
    static int used() {return 0;};
};

typedef __SafeToReturn __ReturnOK;

// Use this to ensure that it is safe to return from a given scope
#define DEBUG_ASSURE_SAFE_TO_RETURN     __ReturnOK::safe_to_return()

// Unfortunately, the only way to make this work is to #define all return statements --
// even the ones at global scope.  This actually generates better code that appears.
// The call is dead, and does not appear in the generated code, even in a checked
// build.  (And, in fastchecked, there is no penalty at all.)
//
#ifdef _MSC_VER
#define debug_instrumented_return if (0 && __ReturnOK::safe_to_return()) { } else return
#else // _MSC_VER
#define debug_instrumented_return for (;1;__ReturnOK::safe_to_return()) return
#endif // _MSC_VER

#define return debug_instrumented_return

#define DEBUG_ASSURE_NO_RETURN_BEGIN(arg) { typedef __YouCannotUseAReturnStatementHere __ReturnOK; if (0 && __ReturnOK::used()) { } else {
#define DEBUG_ASSURE_NO_RETURN_END(arg)   } }

#else // defined(_DEBUG) && !defined(JIT_BUILD) && (!defined(_MSC_FULL_VER) || _MSC_FULL_VER > 190024315)

#define DEBUG_ASSURE_SAFE_TO_RETURN TRUE

#define DEBUG_ASSURE_NO_RETURN_BEGIN(arg) {
#define DEBUG_ASSURE_NO_RETURN_END(arg) }

#endif // defined(_DEBUG) && !defined(JIT_BUILD) && (!defined(_MSC_FULL_VER) || _MSC_FULL_VER > 190024315)

#endif  // _DEBUGRETURN_H_
