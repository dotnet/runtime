// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// NSENUMSHANDLEALLCASES.H -
//

//
// Meta-programming to ensure that all NFT cases are properly handled in switch statements that should handle all NFT types
//
// Uses of this header file are done by
//  - #include the header before the case statement, probably at the top of the cpp file
//  - #define NFT_CASE_VERIFICATION_TYPE_NAME(type) before the switch to give a descriptive name based on type. The type name string
//     is the detail that gets used to find the switch statement that has a problem
//  - Instead of using normal case statements, use NFT_CASE(type). See examples in class.cpp.
//  - In a default: case statement, define NFT_VERIFY_ALL_CASES and then include this file again.
//
#ifndef NSENUMHANDLEALLCASES_H
#define NSENUMHANDLEALLCASES_H

// Requiring all nft types to be handled is done by defining a variable in each case statement, and then in the default: statement
// computing a value that depends on the value of all of those variables.

#ifdef _DEBUG
#define NFT_CASE(type) case type: int NFT_CASE_VERIFICATION_TYPE_NAME(type);

#else
#define NFT_CASE(type) case type:
#endif
#endif // NSENUMHANDLEALLCASES_H

#if defined(_DEBUG) && defined(NFT_VERIFY_ALL_CASES)

int *nftAccumulator = nullptr;
do {
#undef DEFINE_NFT
#define DEFINE_NFT(type, size, WinRTSupported) nftAccumulator += (int)&NFT_CASE_VERIFICATION_TYPE_NAME(type),
#include "nsenums.h"
nftAccumulator = nullptr;
} while (false);
#undef DEFINE_NFT
#endif // _DEBUG && NFT_VERIFY_ALL_CASES
#undef NFT_VERIFY_ALL_CASES
#undef NFT_CASE_VERIFICATION_TYPE_NAME
