// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: StringNative.h
//

//
// Purpose: Contains types and method signatures for the String class
//

//

#include "fcall.h"
#include "qcall.h"
#include "excep.h"

#ifndef _STRINGNATIVE_H_
#define _STRINGNATIVE_H_
//
// Each function that we call through native only gets one argument,
// which is actually a pointer to it's stack of arguments.  Our structs
// for accessing these are defined below.
//

//
//These are the type signatures for String
//
//
// The method signatures for each of the methods we define.
// N.B.: There's a one-to-one mapping between the method signatures and the
// type definitions given above.
//


// Compile the string functionality with these pragma flags (equivalent of the command line /Ox flag)
// Compiling this functionality differently gives us significant throughout gain in some cases.
#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize("tgy", on)
#endif

class COMString {
public:
    //
    // Search/Query Methods
    //
    static FCDECL1(FC_BOOL_RET, IsFastSort, StringObject* pThisRef);
    static FCDECL1(FC_BOOL_RET, IsAscii, StringObject* pThisRef);

    static FCDECL2(INT32, FCCompareOrdinalIgnoreCaseWC, StringObject* strA, __in_z INT8 *strB);

    static FCDECL6(INT32, CompareOrdinalEx, StringObject* strA, INT32 indexA, INT32 countA, StringObject* strB, INT32 indexB, INT32 countB);

    static FCDECL2(FC_CHAR_RET, GetCharAt, StringObject* pThisRef, INT32 index);
    static FCDECL1(INT32, Length, StringObject* pThisRef);

    //
    // Interop
    //
    static FCDECL2(FC_BOOL_RET, FCTryGetTrailByte, StringObject* thisRefUNSAFE, UINT8 *pbData);
    static FCDECL2(VOID,        FCSetTrailByte,    StringObject* thisRefUNSAFE, UINT8 bData);
};

// Revert to command line compilation flags
#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize ("", on)
#endif

#endif // _STRINGNATIVE_H_






