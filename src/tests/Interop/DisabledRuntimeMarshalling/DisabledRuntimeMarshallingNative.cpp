// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>

struct StructWithShortAndBool
{
    bool b;
    short s;
    // Make sure we don't have any cases where the native code could return a value of this type one way,
    // but an invalid managed declaration would expect it differently. This ensures that test failures won't be
    // due to crashes from a mismatched return scheme in the calling convention.
    int padding;
};

struct StructWithWCharAndShort
{
    short s;
    WCHAR c;
};

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE CheckStructWithShortAndBool(StructWithShortAndBool str, short s, bool b)
{
    return str.s == s && str.b == b;
}

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE CheckStructWithWCharAndShort(StructWithWCharAndShort str, short s, WCHAR c)
{
    return str.s == s && str.c == c;
}

using CheckStructWithShortAndBoolCallback = bool (STDMETHODCALLTYPE*)(StructWithShortAndBool, short, bool);

extern "C" DLL_EXPORT CheckStructWithShortAndBoolCallback STDMETHODCALLTYPE GetStructWithShortAndBoolCallback()
{
    return &CheckStructWithShortAndBool;
}

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE CallCheckStructWithShortAndBoolCallback(CheckStructWithShortAndBoolCallback cb, StructWithShortAndBool str, short s, bool b)
{
    return cb(str, s, b);
}

extern "C" DLL_EXPORT BYTE PassThrough(BYTE b)
{
    return b;
}

extern "C" DLL_EXPORT void Invalid(...) {}


extern "C" DLL_EXPORT bool STDMETHODCALLTYPE CheckStructWithShortAndBoolWithVariantBool(StructWithShortAndBool str, short s, VARIANT_BOOL b)
{
    // Specifically use VARIANT_TRUE here as invalid marshalling (in the "disabled runtime marshalling" case) will incorrectly marshal VARAINT_TRUE
    // but could accidentally marshal VARIANT_FALSE correctly since it is 0, which is the same representation as a zero or sign extension of the C# false value.
    return str.s == s && str.b == (b == VARIANT_TRUE);
}

using CheckStructWithShortAndBoolWithVariantBoolCallback = bool (STDMETHODCALLTYPE*)(StructWithShortAndBool, short, VARIANT_BOOL);

extern "C" DLL_EXPORT CheckStructWithShortAndBoolWithVariantBoolCallback STDMETHODCALLTYPE GetStructWithShortAndBoolWithVariantBoolCallback()
{
    return &CheckStructWithShortAndBoolWithVariantBool;
}
