// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>

struct StructWithShortAndBool
{
    short s;
    bool b;
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

extern "C" DLL_EXPORT BYTE PassThrough(BYTE b)
{
    return b;
}

extern "C" DLL_EXPORT void Invalid(...) {}

#ifdef _WIN32
extern "C" DLL_EXPORT bool STDMETHODCALLTYPE CheckStructWithShortAndBoolWithVariantBool(StructWithShortAndBool str, short s, VARIANT_BOOL b)
{
    // Specifically use VARIANT_TRUE here as invalid marshalling (in the "disabled runtime marshalling" case) will incorrectly marshal VARAINT_TRUE
    // but could accidentally marshal VARIANT_FALSE correctly since it is 0, which is the same representation as a zero or sign extension of the C# false value.
    return str.s == s && str.b == (b == VARIANT_TRUE);
}
#endif
