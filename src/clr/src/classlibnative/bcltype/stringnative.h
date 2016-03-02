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
    // Constructors
    //
    static FCDECL5(Object *, StringInitSBytPtrPartialEx, StringObject *thisString,
                   I1 *ptr, INT32 startIndex, INT32 length, Object* encoding);
    static FCDECL2(Object *, StringInitCharPtr, StringObject *stringThis, INT8 *ptr);
    static FCDECL4(Object *, StringInitCharPtrPartial, StringObject *stringThis, INT8 *value,
                   INT32 startIndex, INT32 length);

    //
    // Search/Query Methods
    //
    static FCDECL1(FC_BOOL_RET, IsFastSort, StringObject* pThisRef);
    static FCDECL1(FC_BOOL_RET, IsAscii, StringObject* pThisRef);

    static FCDECL2(INT32, FCCompareOrdinalIgnoreCaseWC, StringObject* strA, __in_z INT8 *strB);

    static FCDECL5(INT32, CompareOrdinalEx, StringObject* strA, INT32 indexA, StringObject* strB, INT32 indexB, INT32 count);

    static FCDECL4(INT32, IndexOfChar, StringObject* vThisRef, CLR_CHAR value, INT32 startIndex, INT32 count );

    static FCDECL4(INT32, LastIndexOfChar, StringObject* thisRef, CLR_CHAR value, INT32 startIndex, INT32 count );

    static FCDECL4(INT32, LastIndexOfCharArray, StringObject* thisRef, CHARArray* valueRef, INT32 startIndex, INT32 count );

    static FCDECL4(INT32, IndexOfCharArray, StringObject* vThisRef, CHARArray* value, INT32 startIndex, INT32 count );

    static FCDECL2(FC_CHAR_RET, GetCharAt, StringObject* pThisRef, INT32 index);
    static FCDECL1(INT32, Length, StringObject* pThisRef);

    //
    // Modifiers
    //
    static FCDECL4(Object*, PadHelper, StringObject* thisRefUNSAFE, INT32 totalWidth, CLR_CHAR paddingChar, CLR_BOOL isRightPadded);

    static FCDECL3(Object*, ReplaceString, StringObject* thisRef, StringObject* oldValue, StringObject* newValue);

    static FCDECL3(Object*, Insert, StringObject* thisRefUNSAFE, INT32 startIndex, StringObject* valueUNSAFE);

    static FCDECL3(Object*, Remove, StringObject* thisRefUNSAFE, INT32 startIndex, INT32 count);

    //
    // Interop
    //
#ifdef FEATURE_COMINTEROP
    static FCDECL2(FC_BOOL_RET, FCTryGetTrailByte, StringObject* thisRefUNSAFE, UINT8 *pbData);
    static FCDECL2(VOID,        FCSetTrailByte,    StringObject* thisRefUNSAFE, UINT8 bData);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    static FCDECL3(INT32, Marvin32HashString, StringObject* thisRefUNSAFE, INT32 strLen, INT64 additionalEntropy);
    static BOOL QCALLTYPE UseRandomizedHashing();
#endif // FEATURE_RANDOMIZED_STRING_HASHING

};

// Revert to command line compilation flags
#if defined(_MSC_VER) && defined(_TARGET_X86_)
#pragma optimize ("", on)
#endif

#endif // _STRINGNATIVE_H_






