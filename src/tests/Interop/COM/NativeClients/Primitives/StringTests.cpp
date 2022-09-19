// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ClientTests.h"
#include <platformdefines.h>
#include <vector>
#include <sstream>

namespace
{
    template
    <
        typename CT,
        void*(*STR_ALLOC)(size_t),
        void(*STR_FREE)(void*)
    >
    class AnyStr
    {
    public:
        AnyStr() : _lenBytes{ 0 } , _str{ nullptr }
        { }

        explicit AnyStr(_In_z_ const char *str)
            : _lenBytes{ (::strlen(str) + 1) * sizeof(CT) }
        {
            _str = (CT *)STR_ALLOC(_lenBytes);
            CT *strLocal = _str;

            while (*str)
            {
                // [TODO] handle UTF8
                *strLocal = static_cast<CT>(*str++);
                strLocal++;
            }
            *strLocal = CT{ '\0' };
        }

        // Concat strings
        AnyStr(_In_ const AnyStr &l, _In_ const AnyStr &r)
            : _lenBytes{ l._lenBytes + r._lenBytes - sizeof(CT) } // Remove duplicate null
        {
            _str = (CT *)STR_ALLOC(_lenBytes);
            CT *strLocal = _str;
            std::memcpy(strLocal, l._str, l._lenBytes - sizeof(CT)); // Ignore null

            size_t l_len = l.Length();
            std::memcpy(strLocal + l_len, r._str, r._lenBytes);
        }

        AnyStr(_In_ const AnyStr &other)
            : _lenBytes{ other._lenBytes }
            , _str{ (CT *)STR_ALLOC(other._lenBytes) }
        {
            std::memcpy(_str, other._str, _lenBytes);
        }

        AnyStr& operator=(_In_ const AnyStr &other)
        {
            AnyStr old{ std::move(*this) };
            AnyStr otherCopy{ other };
            (*this) = std::move(otherCopy);
            return (*this);
        }

        AnyStr(_Inout_ AnyStr &&other)
            : _lenBytes{ other._lenBytes }
            , _str{ other._str }
        {
            other._str = nullptr;
        }

        AnyStr& operator=(_Inout_ AnyStr &&other)
        {
            AnyStr tmp{ std::move(*this) };
            _lenBytes = other._lenBytes;
            _str = other._str;
            other._str = nullptr;
            return (*this);
        }

        ~AnyStr()
        {
            if (_str != nullptr)
                STR_FREE(_str);
        }

        operator CT*()
        {
            return _str;
        }

        operator const CT*() const
        {
            return _str;
        }

        CT** operator &()
        {
            return &_str;
        }

        bool operator==(_In_ const AnyStr &other) const
        {
            return EqualTo(other._str);
        }

        bool operator!=(_In_ const AnyStr &other) const
        {
            return !(*this == other);
        }

        void Attach(_In_z_ CT *data)
        {
            AnyStr tmp{ std::move(*this) };

            CT *dataIter = data;
            int len = 1; // Include 1 for null
            while (*dataIter++)
                ++len;

            _str = data;
            _lenBytes = len * sizeof(CT);
        }

        // String length _not_ including null
        size_t Length() const
        {
            if (_lenBytes == 0)
                return 0;

            return (_lenBytes - sizeof(CT)) / sizeof(CT);
        }

        // String length including null in bytes
        size_t LengthByte() const
        {
            return _lenBytes;
        }

        bool AllAscii() const
        {
            const CT *c = _str;
            const CT MaxAscii = (CT)0x7f;
            while (*c)
            {
                if ((*c++) > MaxAscii)
                    return false;
            }

            return true;
        }

        bool EqualTo(_In_z_ const CT *str) const
        {
            const CT *tmp = str;
            int len = 1; // Include 1 for null
            while (*tmp++)
                ++len;

            if (_lenBytes != (sizeof(CT) * len))
                return false;

            return (0 == std::memcmp(_str, str, _lenBytes));
        }

        HRESULT Reverse(_Inout_ AnyStr &res) const
        {
            AnyStr tmp{};

            if (_lenBytes > 0)
            {
                tmp._lenBytes = _lenBytes;
                tmp._str = (CT *)STR_ALLOC(_lenBytes);
                if (tmp._str == nullptr)
                    return E_OUTOFMEMORY;

                ::memcpy(tmp._str, _str, _lenBytes);
                std::reverse(tmp._str, tmp._str + Length());
            }

            res = std::move(tmp);
            return S_OK;
        }

    private:
        size_t _lenBytes;
        CT *_str;
    };

    // BSTR string
    using BStr = AnyStr<OLECHAR, &CoreClrBStrAlloc, &CoreClrBStrFree>;

    // Wide string
    using WStr = AnyStr<WCHAR, &CoreClrAlloc, &CoreClrFree>;

    // Narrow string
    using NStr = AnyStr<CHAR, &CoreClrAlloc, &CoreClrFree>;

    template <typename STR>
    std::vector<std::pair<STR, STR>> GetAddPairs()
    {
        std::vector<std::pair<STR, STR>> pairs;

        pairs.push_back({ STR{ "" }, STR{ "" } });
        pairs.push_back({ STR{ "" }, STR{ "def" } });
        pairs.push_back({ STR{ "abc" }, STR{ "" } });
        pairs.push_back({ STR{ "abc" }, STR{ "def" } });

        // String marshalling is optimized where strings shorter than MAX_PATH are
        // allocated on the stack. Longer strings have memory allocated for them.
        pairs.push_back({
            STR{ "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901" },
            STR{ "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901" }
            });

        return pairs;
    }

    template <typename STR>
    std::vector<STR> GetReversableStrings()
    {
        std::vector<STR> rev;

        rev.push_back(STR{ "" });
        rev.push_back(STR{ "a" });
        rev.push_back(STR{ "abc" });
        rev.push_back(STR{ "reversible string" });

        // Long string optimization validation
        rev.push_back(STR{ "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901" });

        return rev;
    }

    void Marshal_LPString(_In_ IStringTesting* stringTesting)
    {
        ::printf("Marshal strings as LPStr\n");

        HRESULT hr;

        auto pairs = GetAddPairs<NStr>();
        for (auto &p : pairs)
        {
            if (!p.first.AllAscii() || !p.second.AllAscii())
            {
                // LPStr doesn't support non-ascii characters
                continue;
            }

            LPSTR tmp;
            NStr expected{ p.first, p.second };
            THROW_IF_FAILED(stringTesting->Add_LPStr(p.first, p.second, &tmp));

            NStr actual;
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected.EqualTo(actual));
        }

        auto reversible = GetReversableStrings<NStr>();
        for (const auto &r : reversible)
        {
            if (!r.AllAscii())
            {
                // LPStr doesn't support non-ascii characters
                continue;
            }

            LPSTR tmp;
            NStr local{ r };

            NStr actual;
            NStr expected;
            THROW_IF_FAILED(r.Reverse(expected));

            THROW_IF_FAILED(stringTesting->Reverse_LPStr(local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);

            THROW_IF_FAILED(stringTesting->Reverse_LPStr_Ref(&local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);
            THROW_FAIL_IF_FALSE(r == local); // Local should not be changed

            local = r;
            THROW_IF_FAILED(stringTesting->Reverse_LPStr_InRef(&local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);
            THROW_FAIL_IF_FALSE(r == local); // Local should not be changed

            THROW_IF_FAILED(stringTesting->Reverse_LPStr_Out(local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);

            actual = local;
            tmp = actual;
            THROW_IF_FAILED(stringTesting->Reverse_LPStr_OutAttr(local, tmp)); // No-op for strings
            THROW_FAIL_IF_FALSE(local == actual);
        }
    }

    void Marshal_LPWString(_In_ IStringTesting* stringTesting)
    {
        ::printf("Marshal strings as LPWStr\n");

        HRESULT hr;

        auto pairs = GetAddPairs<WStr>();
        for (auto &p : pairs)
        {
            LPWSTR tmp;
            WStr expected{ p.first, p.second };
            THROW_IF_FAILED(stringTesting->Add_LPWStr(p.first, p.second, &tmp));

            WStr actual;
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected.EqualTo(actual));
        }

        auto reversible = GetReversableStrings<WStr>();
        for (const auto &r : reversible)
        {
            LPWSTR tmp;
            WStr local{ r };

            WStr actual;
            WStr expected;
            THROW_IF_FAILED(r.Reverse(expected));

            THROW_IF_FAILED(stringTesting->Reverse_LPWStr(local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);

            THROW_IF_FAILED(stringTesting->Reverse_LPWStr_Ref(&local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);
            THROW_FAIL_IF_FALSE(r == local); // Local should not be changed

            local = r;
            THROW_IF_FAILED(stringTesting->Reverse_LPWStr_InRef(&local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);
            THROW_FAIL_IF_FALSE(r == local); // Local should not be changed

            THROW_IF_FAILED(stringTesting->Reverse_LPWStr_Out(local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);

            actual = local;
            tmp = actual;
            THROW_IF_FAILED(stringTesting->Reverse_LPWStr_OutAttr(local, tmp)); // No-op for strings
            THROW_FAIL_IF_FALSE(local == actual);
        }
    }

    void Marshal_BStrString(_In_ IStringTesting* stringTesting)
    {
        ::printf("Marshal strings as BStr\n");

        HRESULT hr;

        auto pairs = GetAddPairs<BStr>();
        for (auto &p : pairs)
        {
            BSTR tmp;
            BStr expected{ p.first, p.second };
            THROW_IF_FAILED(stringTesting->Add_BStr(p.first, p.second, &tmp));

            BStr actual;
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected.EqualTo(actual));
        }

        auto reversible = GetReversableStrings<BStr>();
        for (const auto &r : reversible)
        {
            BSTR tmp;
            BStr local{ r };

            BStr actual;
            BStr expected;
            THROW_IF_FAILED(r.Reverse(expected));

            THROW_IF_FAILED(stringTesting->Reverse_BStr(local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);

            THROW_IF_FAILED(stringTesting->Reverse_BStr_Ref(&local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);
            THROW_FAIL_IF_FALSE(r == local); // Local should not be changed

            local = r;
            THROW_IF_FAILED(stringTesting->Reverse_BStr_InRef(&local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);
            THROW_FAIL_IF_FALSE(r == local); // Local should not be changed

            THROW_IF_FAILED(stringTesting->Reverse_BStr_Out(local, &tmp));
            actual.Attach(tmp);
            THROW_FAIL_IF_FALSE(expected == actual);

            actual = local;
            tmp = actual;
            THROW_IF_FAILED(stringTesting->Reverse_BStr_OutAttr(local, tmp)); // No-op for strings
            THROW_FAIL_IF_FALSE(local == actual);
        }
    }

    void Marshal_LCID(_In_ IStringTesting* stringTesting)
    {
        ::printf("Marshal LCIDs\n");

        HRESULT hr;

        LCID lcid = MAKELCID(MAKELANGID(LANG_SPANISH, SUBLANG_SPANISH_CHILE), SORT_DEFAULT);

        WStr r = GetReversableStrings<WStr>()[0];
        WStr local{ r };

        WStr actual;
        WStr expected;
        THROW_IF_FAILED(r.Reverse(expected));

        LPWSTR tmp;
        THROW_IF_FAILED(stringTesting->Reverse_LPWSTR_With_LCID(local, lcid, &tmp));
        actual.Attach(tmp);
        THROW_FAIL_IF_FALSE(expected == actual);

        LCID actualLcid;

        THROW_IF_FAILED(stringTesting->Pass_Through_LCID(lcid, &actualLcid));
        THROW_FAIL_IF_FALSE(lcid == actualLcid);
    }
}

void Run_StringTests()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer"), W("StringTesting") };

    ComSmartPtr<IStringTesting> stringTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_StringTesting, nullptr, CLSCTX_INPROC, IID_IStringTesting, (void**)&stringTesting));

    Marshal_LPString(stringTesting);
    Marshal_LPWString(stringTesting);
    Marshal_BStrString(stringTesting);
    Marshal_LCID(stringTesting);
}
