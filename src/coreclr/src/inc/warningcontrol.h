// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// warningcontrol.h
//
// Header file to globally control the warning settings for the entire Viper build.
// You do not need to explicitly include this file; rather, it gets included
// on the command line with a /FI (force include) directive. This is controlled
// in sources.vip.
//
// KEEP THIS LIST SORTED!
//

#if defined(_MSC_VER)
#pragma warning(disable :4001)   // "nonstandard extension 'single line comment' was used"
#pragma warning(error   :4007)   // 'main' : must be __cdecl
#pragma warning(disable :4010)   // "single-line comment contains line-continuation character"
#pragma warning(error   :4013)   // 'function' undefined - assuming extern returning int
#pragma warning(disable :4022)   // "'%s' : pointer mismatch for actual parameter %d"
#pragma warning(disable :4047)   // "'%$L' : '%$T' differs in levels of indirection from '%$T'"
#pragma warning(disable :4053)   // "one void operand for '?:'"
#pragma warning(disable :4056)   // "overflow in floating-point constant arithmetic"
#pragma warning(disable :4061)   // "enumerate '%$S' in switch of enum '%$S' is not explicitly handled by a case label"
#pragma warning(error   :4071)   // no function prototype given
#pragma warning(error   :4072)   // no function prototype given (fastcall)
#pragma warning(3               :4092)   // sizeof returns 'unsigned long'
#pragma warning(disable :4100)   // "'%$S' : unreferenced formal parameter"
#pragma warning(disable :4101)   // "'%$S' : unreferenced local variable"
//#pragma warning(error :4102)   // "'%$S' : unreferenced label"
#pragma warning(3               :4121)   // structure is sensitive to alignment
#pragma warning(disable :4127)   // "conditional expression is constant"
#pragma warning(3               :4125)   // decimal digit in octal sequence
#pragma warning(3               :4130)   // logical operation on address of string constant
#pragma warning(3               :4132)   // const object should be initialized
#pragma warning(error   :4171)   // no function prototype given (old style)
#pragma warning(4               :4177)   // pragma data_seg s/b at global scope
#pragma warning(disable :4201)   // "nonstandard extension used : nameless struct/union"
#pragma warning(disable :4204)   // "nonstandard extension used : non-constant aggregate initializer"
#pragma warning(4               :4206)   // Source File is empty
#pragma warning(3               :4212)   // function declaration used ellipsis
#pragma warning(error           :4259)   // pure virtual function was not defined
#pragma warning(disable         :4291)   // delete not defined for new, c++ exception may cause leak
#pragma warning(disable         :4302)   // truncation from '%$S' to '%$S'
#pragma warning(disable         :4311)   // pointer truncation from '%$S' to '%$S'
#pragma warning(disable         :4312)   // '<function-style-cast>' : conversion from '%$S' to '%$S' of greater size
#pragma warning(disable :4334)   // result of 32-bit shift implicitly converted to 64 bits
#pragma warning(disable :4345)   // behavior change: an object of POD type constructed with an initializer of the form () will be default-initialized
#pragma warning(disable :4430)   // missing type specifier: C++ doesn't support default-int
#pragma warning(disable :4477)   // format string '%$S' requires an argument of type '%$S', but variadic argument %d has type '%$S'
#pragma warning(3               :4509)   // "nonstandard extension used: '%$S' uses SEH and '%$S' has destructor"
                                                                 //
                                                                 // But beware of doing a return from inside such a try block:
                                                                 //
                                                                 //     int foo()
                                                                 //             {
                                                                 //             ClassWithDestructor c;
                                                                 //             __try {
                                                                 //                     return 0;
                                                                 //             } __finally {
                                                                 //                     printf("in finally");
                                                                 //             }
                                                                 //
                                                                 // as (it's a bug) the return value gets toasted. So DON'T casually
                                                                 // dismiss this warning if you're compiling w/o CXX EH turned on (the default).

#pragma warning(3               :4530)   // C++ exception handler used, but unwind semantics are not enabled. Specify -GX
#pragma warning(error   :4551)   // Function call missing argument list

#pragma warning(error   :4700)   // Local used w/o being initialized
#pragma warning(disable :4706)   // assignment within conditional expression
#pragma warning(disable :4768)   // __declspec attributes before linkage specification are ignored
#pragma warning(error   :4806)   // unsafe operation involving type 'bool'
#pragma warning(disable :4995)   // '_OLD_IOSTREAMS_ARE_DEPRECATED': name was marked as #pragma deprecated

#if defined(_DEBUG) && (!defined(_MSC_FULL_VER) || (_MSC_FULL_VER <= 181040116))
// The CLR header file check.h, macro CHECK_MSG_EX, can create unreachable code if the LEAVE_DEBUG_ONLY_CODE
// macro is not empty (such as it is defined in contract.h) and the _RESULT macro expands to "return".
// Checked-in compilers used by the TFS-based desktop build (e.g., version 18.10.40116.8) started reporting
// unreachable code warnings when debugholder.h was changed to no longer #define "return" to something relatively
// complex. However, newer compilers, such as Visual Studio 2015, used to build the CLR from the open source
// GitHub repo, still do not report this warning. We don't want to disable this warning for open source build,
// which will use a newer compiler. Hence, only disable it for older compilers.
#pragma warning(disable :4702)   // unreachable code
#endif

#endif  // defined(_MSC_VER)
