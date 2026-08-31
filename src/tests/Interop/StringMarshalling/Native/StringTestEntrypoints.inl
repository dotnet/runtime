// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is included by each of the various string testing projects.
// The StringType and Tests type aliases and the FUNCTION_NAME macro
// must be defined for this file to compile.

struct StringInStruct
{
    StringType str;
};

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MatchFunctionName(StringType str)
{
    return Tests::Compare(FUNCTION_NAME, str);
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MatchFunctionNameByRef(StringType* str)
{
    return Tests::Compare(FUNCTION_NAME, *str);
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE MatchFunctionNameInStruct(StringInStruct str)
{
    return Tests::Compare(FUNCTION_NAME, str.str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReverseInplace(StringType str)
{
    Tests::ReverseInplace(str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReverseInplaceByrefInStruct(StringInStruct* str)
{
    Tests::ReverseInplace(str->str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReverseInplaceByref(StringType* str)
{
    Tests::ReverseInplace(*str);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE Reverse(StringType str, StringType* result)
{
    Tests::Reverse(str, result);
}

extern "C" DLL_EXPORT StringType STDMETHODCALLTYPE ReverseAndReturn(StringType str)
{
    StringType result;
    Tests::Reverse(str, &result);
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE VerifyReversed(StringType str, VerifyReversedCallback<StringType> f)
{
    StringType result;
    Tests::Reverse(str, &result);
    BOOL isReversed = f(str, result);
    Tests::FreeString(result);
    return isReversed;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseInCallback(StringType str, ReverseCallback<StringType> f)
{
    StringType callbackResult;
    f(str, &callbackResult);
    StringType nativeResult;
    Tests::Reverse(str, &nativeResult);
    BOOL isReversed = Tests::Compare(nativeResult, callbackResult);
    Tests::FreeString(callbackResult);
    Tests::FreeString(nativeResult);
    return isReversed;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseInplaceInCallback(StringType str, ReverseInplaceCallback<StringType> f)
{
    StringType nativeResult;
    Tests::Reverse(str, &nativeResult);
    f(str);
    BOOL isReversed = Tests::Compare(nativeResult, str);
    Tests::FreeString(nativeResult);
    return isReversed;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReverseInCallbackReturned(StringType str, ReverseCallbackReturned<StringType> f)
{
    StringType callbackResult = f(str);
    StringType nativeResult;
    Tests::Reverse(str, &nativeResult);
    BOOL isReversed = Tests::Compare(nativeResult, callbackResult);
    Tests::FreeString(callbackResult);
    Tests::FreeString(nativeResult);
    return isReversed;
}
