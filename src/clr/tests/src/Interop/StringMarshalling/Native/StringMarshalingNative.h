// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <platformdefines.h>
#include <type_traits>
#include <algorithm>

template<typename StringT>
struct StringTraits
{
    using type = StringT;
    using CharT = typename std::remove_pointer<StringT>::type;
    using ConstStringT = CharT const *;
};

struct BSTRTraits
{
    using type = BSTR;
    using ConstStringT = BSTR;
};

template<typename StringT>
using VerifyReversedCallback = BOOL (__cdecl*)(StringT original, StringT reversed);

template<typename StringT>
using ReverseCallback = void(__cdecl*)(StringT original, StringT* reversed);

template<typename StringT>
using ReverseCallback = void(__cdecl*)(StringT original, StringT* reversed);

template<typename StringT>
using ReverseCallbackReturned = StringT(__cdecl*)(StringT original);

template<typename StringT>
using ReverseInplaceCallback = void(__cdecl*)(StringT str);

template<typename StringTraitsT, size_t LengthFunction(typename StringTraitsT::ConstStringT), typename CharT = typename StringTraitsT::CharT>
struct StringMarshalingTestsBase
{
    using StringT = typename StringTraitsT::type;
    using ConstStringT = typename StringTraitsT::ConstStringT;

    static BOOL Compare(ConstStringT expected, const ConstStringT actual)
    {
        if (LengthFunction(expected) != LengthFunction(actual))
        {
            return FALSE;
        }

        size_t length = LengthFunction(expected) + 1;
        CharT const* currE = (CharT const*)expected;
        CharT const* endE = (CharT const*)expected + length;
        CharT const* currA = (CharT const*)actual;
        CharT const* endA = (CharT const*)actual + length;
        for (; currE != endE; ++currE, ++currA)
        {
            if (*currE != *currA)
                return FALSE;
        }
        
        return TRUE;
    }

    static void ReverseInplace(StringT str)
    {
        std::reverse((CharT*)str, (CharT*)str + LengthFunction(str));
    }
};

template<typename StringT, size_t LengthFunction(typename StringTraits<StringT>::ConstStringT)>
struct StringMarshalingTests : StringMarshalingTestsBase<StringTraits<StringT>, LengthFunction>
{
    using Base = StringMarshalingTestsBase<StringTraits<StringT>, LengthFunction>;

    static void Reverse(StringT str, StringT* result)
    {
        size_t length = LengthFunction(str);
        size_t byteSize = sizeof(typename StringTraits<StringT>::CharT) * (length + 1);
        StringT buffer = (StringT)CoreClrAlloc(byteSize);
        
        memcpy(buffer, str, byteSize);

        Base::ReverseInplace(buffer);
        *result = buffer;
    }

    static void FreeString(StringT str)
    {
        CoreClrFree(str);
    }
};

template<size_t LengthFunction(BSTR), typename CharT, BSTR Alloc(CharT const*, size_t)>
struct BStrMarshalingTests : StringMarshalingTestsBase<BSTRTraits, LengthFunction, CharT>
{
    using Base = StringMarshalingTestsBase<BSTRTraits, LengthFunction, CharT>;
    using StringT = typename Base::StringT;
    static void Reverse(BSTR str, StringT* result)
    {
        size_t length = LengthFunction(str);
        StringT buffer = Alloc((CharT const*)str, length);

        Base::ReverseInplace(buffer);
        *result = buffer;
    }

    static void FreeString(StringT str)
    {
        CoreClrBStrFree(str);
    }
};

// We the length function needs to match the default calling convention. Since CoreCLR builds with stdcall as the default on Windows, the built-in strlen function is the wrong calling convention.
// Provide a simple wrapper here that uses the default calling convention to enable tests to use strlen.
size_t default_callconv_strlen(const char* str)
{
    return strlen(str);
}
