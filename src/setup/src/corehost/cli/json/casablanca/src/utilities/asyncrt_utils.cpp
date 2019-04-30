/***
* ==++==
*
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* ==--==
* =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
*
* Utilities
*
* For the latest on this and related APIs, please see: https://github.com/Microsoft/cpprestsdk
*
* =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
****/

#include "stdafx.h"

#ifndef _WIN32
#if defined(__clang__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunused-local-typedef"
#endif
#if defined(__clang__)
#pragma clang diagnostic pop
#endif
#endif

// Could use C++ standard library if not __GLIBCXX__,
// For testing purposes we just the handwritten on all platforms.
#if defined(CPPREST_STDLIB_UNICODE_CONVERSIONS)
#include <codecvt>
#endif

using namespace web;
using namespace utility;
using namespace utility::conversions;

namespace utility
{
namespace details
{

#if !defined(ANDROID) && !defined(__ANDROID__)
static std::once_flag g_c_localeFlag;
static std::unique_ptr<scoped_c_thread_locale::xplat_locale, void(*)(scoped_c_thread_locale::xplat_locale *)> g_c_locale(nullptr, [](scoped_c_thread_locale::xplat_locale *){});
scoped_c_thread_locale::xplat_locale scoped_c_thread_locale::c_locale()
{
    std::call_once(g_c_localeFlag, [&]()
    {
        scoped_c_thread_locale::xplat_locale *clocale = new scoped_c_thread_locale::xplat_locale();
#ifdef _WIN32
        *clocale = _create_locale(LC_ALL, "C");
        if (*clocale == nullptr)
        {
            throw std::runtime_error("Unable to create 'C' locale.");
        }
        auto deleter = [](scoped_c_thread_locale::xplat_locale *clocale)
        {
            _free_locale(*clocale);
            delete clocale;
        };
#else
        *clocale = newlocale(LC_ALL, "C", nullptr);
        if (*clocale == nullptr)
        {
            throw std::runtime_error("Unable to create 'C' locale.");
        }
        auto deleter = [](scoped_c_thread_locale::xplat_locale *clocale)
        {
            freelocale(*clocale);
            delete clocale;
        };
#endif
        g_c_locale = std::unique_ptr<scoped_c_thread_locale::xplat_locale, void(*)(scoped_c_thread_locale::xplat_locale *)>(clocale, deleter);
    });
    return *g_c_locale;
}
#endif

#ifdef _WIN32
scoped_c_thread_locale::scoped_c_thread_locale()
    : m_prevLocale(), m_prevThreadSetting(-1)
{
    char *prevLocale = setlocale(LC_ALL, nullptr);
    if (prevLocale == nullptr)
    {
        throw std::runtime_error("Unable to retrieve current locale.");
    }

    if (std::strcmp(prevLocale, "C") != 0)
    {
        m_prevLocale = prevLocale;
        m_prevThreadSetting = _configthreadlocale(_ENABLE_PER_THREAD_LOCALE);
        if (m_prevThreadSetting == -1)
        {
            throw std::runtime_error("Unable to enable per thread locale.");
        }
        if (setlocale(LC_ALL, "C") == nullptr)
        {
             _configthreadlocale(m_prevThreadSetting);
             throw std::runtime_error("Unable to set locale");
        }
    }
}

scoped_c_thread_locale::~scoped_c_thread_locale()
{
    if (m_prevThreadSetting != -1)
    {
        setlocale(LC_ALL, m_prevLocale.c_str());
        _configthreadlocale(m_prevThreadSetting);
    }
}
#elif (defined(ANDROID) || defined(__ANDROID__))
scoped_c_thread_locale::scoped_c_thread_locale() {}
scoped_c_thread_locale::~scoped_c_thread_locale() {}
#else
scoped_c_thread_locale::scoped_c_thread_locale()
    : m_prevLocale(nullptr)
{
    char *prevLocale = setlocale(LC_ALL, nullptr);
    if (prevLocale == nullptr)
    {
        throw std::runtime_error("Unable to retrieve current locale.");
    }

    if (std::strcmp(prevLocale, "C") != 0)
    {
        m_prevLocale = uselocale(c_locale());
        if (m_prevLocale == nullptr)
        {
            throw std::runtime_error("Unable to set locale");
        }
    }
}

scoped_c_thread_locale::~scoped_c_thread_locale()
{
    if (m_prevLocale != nullptr)
    {
        uselocale(m_prevLocale);
    }
}
#endif
}

namespace details
{

const std::error_category & __cdecl platform_category()
{
#ifdef _WIN32
    return windows_category();
#else
    return linux_category();
#endif
}

#ifdef _WIN32

// Remove once VS 2013 is no longer supported.
#if _MSC_VER < 1900
static details::windows_category_impl instance;
#endif
const std::error_category & __cdecl windows_category()
{
#if _MSC_VER >= 1900
    static details::windows_category_impl instance;
#endif
    return instance;
}

std::string windows_category_impl::message(int errorCode) const CPPREST_NOEXCEPT
{
    const size_t buffer_size = 4096;
    DWORD dwFlags = FORMAT_MESSAGE_FROM_SYSTEM;
    LPCVOID lpSource = NULL;

#if !defined(__cplusplus_winrt)
    if (errorCode >= 12000)
    {
        dwFlags = FORMAT_MESSAGE_FROM_HMODULE;
        lpSource = GetModuleHandleA("winhttp.dll"); // this handle DOES NOT need to be freed
    }
#endif

    std::wstring buffer;
    buffer.resize(buffer_size);

    const auto result = ::FormatMessageW(
        dwFlags,
        lpSource,
        errorCode,
        0,
        &buffer[0],
        buffer_size,
        NULL);
    if (result == 0)
    {
        std::ostringstream os;
        os << "Unable to get an error message for error code: " << errorCode << ".";
        return os.str();
    }

    return utility::conversions::to_utf8string(buffer);
}

std::error_condition windows_category_impl::default_error_condition(int errorCode) const CPPREST_NOEXCEPT
{
    // First see if the STL implementation can handle the mapping for common cases.
    const std::error_condition errCondition = std::system_category().default_error_condition(errorCode);
    const std::string errConditionMsg = errCondition.message();
    if(_stricmp(errConditionMsg.c_str(), "unknown error") != 0)
    {
        return errCondition;
    }

    switch(errorCode)
    {
#ifndef __cplusplus_winrt
    case ERROR_WINHTTP_TIMEOUT:
        return std::errc::timed_out;
    case ERROR_WINHTTP_CANNOT_CONNECT:
        return std::errc::host_unreachable;
    case ERROR_WINHTTP_CONNECTION_ERROR:
        return std::errc::connection_aborted;
#endif
    case INET_E_RESOURCE_NOT_FOUND:
    case INET_E_CANNOT_CONNECT:
        return std::errc::host_unreachable;
    case INET_E_CONNECTION_TIMEOUT:
        return std::errc::timed_out;
    case INET_E_DOWNLOAD_FAILURE:
        return std::errc::connection_aborted;
    default:
        break;
    }

    return std::error_condition(errorCode, *this);
}

#else

const std::error_category & __cdecl linux_category()
{
    // On Linux we are using boost error codes which have the exact same
    // mapping and are equivalent with std::generic_category error codes.
    return std::generic_category();
}

#endif

}

#define LOW_3BITS 0x7
#define LOW_4BITS 0xF
#define LOW_5BITS 0x1F
#define LOW_6BITS 0x3F
#define BIT4 0x8
#define BIT5 0x10
#define BIT6 0x20
#define BIT7 0x40
#define BIT8 0x80
#define L_SURROGATE_START 0xDC00
#define L_SURROGATE_END 0xDFFF
#define H_SURROGATE_START 0xD800
#define H_SURROGATE_END 0xDBFF
#define SURROGATE_PAIR_START 0x10000

utf16string __cdecl conversions::utf8_to_utf16(const std::string &s)
{
#if defined(CPPREST_STDLIB_UNICODE_CONVERSIONS)
    std::wstring_convert<std::codecvt_utf8_utf16<utf16char>, utf16char> conversion;
    return conversion.from_bytes(src);
#else
    utf16string dest;
    // Save repeated heap allocations, use less than source string size assuming some
    // of the characters are not just ASCII and collapse.
    dest.reserve(static_cast<size_t>(static_cast<double>(s.size()) * .70));
    
    for (auto src = s.begin(); src != s.end(); ++src)
    {
        if ((*src & BIT8) == 0) // single byte character, 0x0 to 0x7F
        {
            dest.push_back(utf16string::value_type(*src));
        }
        else
        {
            unsigned char numContBytes = 0;
            uint32_t codePoint;
            if ((*src & BIT7) == 0)
            {
                throw std::range_error("UTF-8 string character can never start with 10xxxxxx");
            }
            else if ((*src & BIT6) == 0) // 2 byte character, 0x80 to 0x7FF
            {
                codePoint = *src & LOW_5BITS;
                numContBytes = 1;
            }
            else if ((*src & BIT5) == 0) // 3 byte character, 0x800 to 0xFFFF
            {
                codePoint = *src & LOW_4BITS;
                numContBytes = 2;
            }
            else if ((*src & BIT4) == 0) // 4 byte character, 0x10000 to 0x10FFFF
            {
                codePoint = *src & LOW_3BITS;
                numContBytes = 3;
            }
            else
            {
                throw std::range_error("UTF-8 string has invalid Unicode code point");
            }

            for (unsigned char i = 0; i < numContBytes; ++i)
            {
                if (++src == s.end())
                {
                    throw std::range_error("UTF-8 string is missing bytes in character");
                }
                if ((*src & BIT8) == 0 || (*src & BIT7) != 0)
                {
                    throw std::range_error("UTF-8 continuation byte is missing leading byte");
                }
                codePoint <<= 6;
                codePoint |= *src & LOW_6BITS;
            }

            if (codePoint >= SURROGATE_PAIR_START)
            {
                // In UTF-16 U+10000 to U+10FFFF are represented as two 16-bit code units, surrogate pairs.
                //  - 0x10000 is subtracted from the code point
                //  - high surrogate is 0xD800 added to the top ten bits
                //  - low surrogate is 0xDC00 added to the low ten bits
                codePoint -= SURROGATE_PAIR_START;
                dest.push_back(utf16string::value_type((codePoint >> 10) | H_SURROGATE_START));
                dest.push_back(utf16string::value_type((codePoint & 0x3FF) | L_SURROGATE_START));
            }
            else
            {
                // In UTF-16 U+0000 to U+D7FF and U+E000 to U+FFFF are represented exactly as the Unicode code point value.
                // U+D800 to U+DFFF are not valid characters, for simplicity we assume they are not present but will encode
                // them if encountered.
                dest.push_back(utf16string::value_type(codePoint));
            }
        }
    }
    return dest;
#endif
}

std::string __cdecl conversions::utf16_to_utf8(const utf16string &w)
{
 #if defined(CPPREST_STDLIB_UNICODE_CONVERSIONS)
     std::wstring_convert<std::codecvt_utf8_utf16<utf16char>, utf16char> conversion;
     return conversion.to_bytes(w);
 #else
    std::string dest;
    dest.reserve(w.size());
    for (auto src = w.begin(); src != w.end(); ++src)
    {
        // Check for high surrogate.
        if (*src >= H_SURROGATE_START && *src <= H_SURROGATE_END)
        {
            const auto highSurrogate = *src++;
            if (src == w.end())
            {
                throw std::range_error("UTF-16 string is missing low surrogate");
            }
            const auto lowSurrogate = *src;
            if (lowSurrogate < L_SURROGATE_START || lowSurrogate > L_SURROGATE_END)
            {
                throw std::range_error("UTF-16 string has invalid low surrogate");
            }

            // To get from surrogate pair to Unicode code point:
            // - subract 0xD800 from high surrogate, this forms top ten bits
            // - subract 0xDC00 from low surrogate, this forms low ten bits
            // - add 0x10000
            // Leaves a code point in U+10000 to U+10FFFF range.
            uint32_t codePoint = highSurrogate - H_SURROGATE_START;
            codePoint <<= 10;
            codePoint |= lowSurrogate - L_SURROGATE_START;
            codePoint += SURROGATE_PAIR_START;

            // 4 bytes need using 21 bits
            dest.push_back(char((codePoint >> 18) | 0xF0));                 // leading 3 bits
            dest.push_back(char(((codePoint >> 12) & LOW_6BITS) | BIT8));   // next 6 bits
            dest.push_back(char(((codePoint >> 6) & LOW_6BITS) | BIT8));    // next 6 bits
            dest.push_back(char((codePoint & LOW_6BITS) | BIT8));           // trailing 6 bits
        }
        else
        {
            if (*src <= 0x7F) // single byte character
            {
                dest.push_back(static_cast<char>(*src));
            }
            else if (*src <= 0x7FF) // 2 bytes needed (11 bits used)
            {
                dest.push_back(char((*src >> 6) | 0xC0));               // leading 5 bits
                dest.push_back(char((*src & LOW_6BITS) | BIT8));        // trailing 6 bits
            }
            else // 3 bytes needed (16 bits used)
            {
                dest.push_back(char((*src >> 12) | 0xE0));              // leading 4 bits
                dest.push_back(char(((*src >> 6) & LOW_6BITS) | BIT8)); // middle 6 bits
                dest.push_back(char((*src & LOW_6BITS) | BIT8));        // trailing 6 bits
            }
        }
    }

    return dest;
 #endif
}

utf16string __cdecl conversions::usascii_to_utf16(const std::string &s)
{
    // Ascii is a subset of UTF-8 so just convert to UTF-16
    return utf8_to_utf16(s);
}

utf16string __cdecl conversions::latin1_to_utf16(const std::string &s)
{
    // Latin1 is the first 256 code points in Unicode.
    // In UTF-16 encoding each of these is represented as exactly the numeric code point.
    utf16string dest;
    dest.resize(s.size());
    for (size_t i = 0; i < s.size(); ++i)
    {
        dest[i] = utf16char(s[i]);
    }
    return dest;
}

utf8string __cdecl conversions::latin1_to_utf8(const std::string &s)
{
    return utf16_to_utf8(latin1_to_utf16(s));
}

utility::string_t __cdecl conversions::to_string_t(utf16string &&s)
{
#ifdef _UTF16_STRINGS
    return std::move(s);
#else
    return utf16_to_utf8(std::move(s));
#endif
}

utility::string_t __cdecl conversions::to_string_t(std::string &&s)
{
#ifdef _UTF16_STRINGS
    return utf8_to_utf16(std::move(s));
#else
    return std::move(s);
#endif
}

utility::string_t __cdecl conversions::to_string_t(const utf16string &s)
{
#ifdef _UTF16_STRINGS
    return s;
#else
    return utf16_to_utf8(s);
#endif
}

utility::string_t __cdecl conversions::to_string_t(const std::string &s)
{
#ifdef _UTF16_STRINGS
    return utf8_to_utf16(s);
#else
    return s;
#endif
}

std::string __cdecl conversions::to_utf8string(std::string value) { return value; }

std::string __cdecl conversions::to_utf8string(const utf16string &value) { return utf16_to_utf8(value); }

utf16string __cdecl conversions::to_utf16string(const std::string &value) { return utf8_to_utf16(value); }

utf16string __cdecl conversions::to_utf16string(utf16string value) { return value; }

static bool is_digit(utility::char_t c) { return c >= _XPLATSTR('0') && c <= _XPLATSTR('9'); }

}
