// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../Native/StringMarshalingNative.h"

using StringType = LPWSTR;
using Tests = StringMarshalingTests<StringType, TP_slen>;

#define FUNCTION_NAME __FUNCTIONW__

#include "../Native/StringTestEntrypoints.inl"

// Verify that we append extra null terminators to our StringBuilder native buffers.
// Although this is a hidden implementation detail, it would be breaking behavior to stop doing this
// so we have a test for it. In particular, this detail prevents us from optimizing marshalling StringBuilders by pinning.
extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Verify_NullTerminators_PastEnd(LPCWSTR buffer, int length)
{
    return buffer[length+1] == W('\0');
}

struct ByValStringInStructAnsi
{
    char str[20];
};

struct ByValStringInStructUnicode
{
    WCHAR str[20];
};

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MatchFuncNameAnsi(ByValStringInStructAnsi str)
{
    return StringMarshalingTests<char*, default_callconv_strlen>::Compare(__func__, str.str);
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MatchFuncNameUni(ByValStringInStructUnicode str)
{
    return StringMarshalingTests<LPWSTR, TP_slen>::Compare(__FUNCTIONW__, str.str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReverseByValStringAnsi(ByValStringInStructAnsi* str)
{
    StringMarshalingTests<char*, default_callconv_strlen>::ReverseInplace(str->str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReverseByValStringUni(ByValStringInStructUnicode* str)
{
    StringMarshalingTests<LPWSTR, TP_slen>::ReverseInplace(str->str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReverseCopyByValStringAnsi(ByValStringInStructAnsi str, ByValStringInStructAnsi* out)
{
    *out = str;
    StringMarshalingTests<char*, default_callconv_strlen>::ReverseInplace(out->str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReverseCopyByValStringUni(ByValStringInStructUnicode str, ByValStringInStructUnicode* out)
{
    *out = str;
    StringMarshalingTests<LPWSTR, TP_slen>::ReverseInplace(out->str);
}
