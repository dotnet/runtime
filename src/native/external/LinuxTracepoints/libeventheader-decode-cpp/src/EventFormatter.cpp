// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#ifndef _FILE_OFFSET_BITS
#define _FILE_OFFSET_BITS 64
#endif

#ifndef _TIME_BITS
#define _TIME_BITS 64
#endif

#include <eventheader/EventFormatter.h>
#include <tracepoint/PerfEventMetadata.h>
#include <tracepoint/PerfEventSessionInfo.h>
#include <tracepoint/PerfEventInfo.h>
#include <tracepoint/PerfByteReader.h>
#include <tracepoint/PerfEventAbi.h>

#include <assert.h>
#include <math.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <inttypes.h>
#include <stdint.h>

#ifdef _WIN32

#include <windows.h>
#include <sal.h>
#define bswap_16(u16) _byteswap_ushort(u16)
#define bswap_32(u32) _byteswap_ulong(u32)
#define bswap_64(u64) _byteswap_uint64(u64)
#define be16toh(u16) _byteswap_ushort(u16)

#else // _WIN32

#include <byteswap.h>
#include <arpa/inet.h>

#endif // _WIN32

#ifndef UNALIGNED
#define UNALIGNED __attribute__((aligned(1)))
#endif

#ifdef _Printf_format_string_
#define _Printf_format_func_(formatIndex, argIndex)
#else
#define _Printf_format_string_
#define _Printf_format_func_(formatIndex, argIndex) \
    __attribute__((__format__(__printf__, formatIndex, argIndex)))
#endif

#ifndef __fallthrough
#define __fallthrough __attribute__((__fallthrough__))
#endif

using namespace std::string_view_literals;
using namespace eventheader_decode;
using namespace tracepoint_decode;

struct SwapNo
{
    uint8_t operator()(uint8_t val) const { return val; }
    uint16_t operator()(uint16_t val) const { return val; }
    uint32_t operator()(uint32_t val) const { return val; }
};

struct SwapYes
{
    uint16_t operator()(uint16_t val) const { return bswap_16(val); }
    uint32_t operator()(uint32_t val) const { return bswap_32(val); }
};

class StringBuilder
{
    char* m_pDest;
    char const* m_pDestEnd;
    std::string& m_dest;
    size_t m_destCommitSize;
    bool const m_wantJsonSpace;
    bool const m_wantFieldTag;
    bool m_needJsonComma;

#ifdef NDEBUG

#define WriteBegin(cchWorstCase) ((void)0)
#define WriteEnd() ((void)0)

#else // NDEBUG

#define WriteBegin(cchWorstCase) \
    char const* const _pLimit = m_pDest + (cchWorstCase); \
    assert(m_pDest <= _pLimit); \
    assert(_pLimit <= m_pDestEnd)
#define WriteEnd() \
    assert(m_pDest <= _pLimit); \
    assert(_pLimit <= m_pDestEnd)

#endif // NDEBUG

public:

    StringBuilder(StringBuilder const&) = delete;
    void operator=(StringBuilder const&) = delete;

    ~StringBuilder()
    {
        AssertInvariants();
        m_dest.erase(m_destCommitSize);
    }

    StringBuilder(std::string& dest, EventFormatterJsonFlags jsonFlags) noexcept
        : m_pDest(dest.data() + dest.size())
        , m_pDestEnd(dest.data() + dest.size())
        , m_dest(dest)
        , m_destCommitSize(dest.size())
        , m_wantJsonSpace(jsonFlags & EventFormatterJsonFlags_Space)
        , m_wantFieldTag(jsonFlags & EventFormatterJsonFlags_FieldTag)
        , m_needJsonComma(false)
    {
        AssertInvariants();
    }

    bool
    WantFieldTag() const noexcept
    {
        return m_wantFieldTag;
    }

    size_t Room() const noexcept
    {
        return m_pDestEnd - m_pDest;
    }

    void
    EnsureRoom(size_t roomNeeded) noexcept(false)
    {
        if (static_cast<size_t>(m_pDestEnd - m_pDest) < roomNeeded)
        {
            GrowRoom(roomNeeded);
        }
    }

    void
    Commit() noexcept
    {
        AssertInvariants();
        m_destCommitSize = m_pDest - m_dest.data();
    }

    // Requires: there is room for utf8.size() chars.
    void
    WriteUtf8(std::string_view utf8) noexcept
    {
        WriteBegin(utf8.size());
        memcpy(m_pDest, utf8.data(), utf8.size());
        m_pDest += utf8.size();
        WriteEnd();
    }

    // Requires: there is room for 1 char.
    void
    WriteUtf8Byte(uint8_t utf8Byte) noexcept
    {
        assert(m_pDest < m_pDestEnd);
        *m_pDest++ = utf8Byte;
    }

    // Requires: there is room for 1 char.
    // Writes 0..1 chars, either [] or ["].
    void
    WriteQuoteIf(bool condition) noexcept
    {
        assert(m_pDest < m_pDestEnd);
        *m_pDest = '"';
        m_pDest += condition;
    }

    // Requires: there is room for 7 chars.
    // Writes: 1..7 UTF-8 bytes, e.g. [a].
    void
    WriteUcsChar(uint32_t ucs4) noexcept
    {
        WriteBegin(7);

        if (ucs4 >= 0x80)
        {
            WriteUcsNonAsciiChar(ucs4);
        }
        else
        {
            *m_pDest++ = static_cast<uint8_t>(ucs4);
        }

        WriteEnd();
    }

    // Requires: there is room for 7 chars.
    // Requires: nonAsciiUcs4 >= 0x80.
    // Writes: 2..7 UTF-8 bytes.
    void
    WriteUcsNonAsciiChar(uint32_t nonAsciiUcs4) noexcept
    {
        WriteBegin(7);
        assert(nonAsciiUcs4 >= 0x80);

        // Note that this algorithm intentionally accepts non-compliant data
        // (surrogates and values above 0x10FFFF). We want to accurately
        // forward exactly what we found in the event.

        if (nonAsciiUcs4 < 0x800)
        {
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 6)) | 0xc0);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4) & 0x3f) | 0x80);
        }
        else if (nonAsciiUcs4 < 0x10000)
        {
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 12)) | 0xe0);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 6) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4) & 0x3f) | 0x80);
        }
        else if (nonAsciiUcs4 < 0x200000)
        {
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 18)) | 0xf0);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 12) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 6) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4) & 0x3f) | 0x80);
        }
        else if (nonAsciiUcs4 < 0x4000000)
        {
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 24)) | 0xf8);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 18) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 12) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 6) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4) & 0x3f) | 0x80);
        }
        else if (nonAsciiUcs4 < 0x80000000)
        {
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 30)) | 0xfc);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 24) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 18) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 12) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 6) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4) & 0x3f) | 0x80);
        }
        else
        {
            *m_pDest++ = static_cast<uint8_t>(0xfe);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 30) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 24) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 18) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 12) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4 >> 6) & 0x3f) | 0x80);
            *m_pDest++ = static_cast<uint8_t>(((nonAsciiUcs4) & 0x3f) | 0x80);
        }

        WriteEnd();
    }

    // Requires: there is room for 2 chars.
    // Writes 2 chars, e.g. [7f].
    void
    WriteHexByte(uint8_t val) noexcept
    {
        WriteBegin(2);
        static char const* const digits = "0123456789abcdef";
        *m_pDest++ = digits[val >> 4];
        *m_pDest++ = digits[val & 0xf];
        WriteEnd();
    }

    // Requires: there is room for cchWorstCase chars.
    // Requires: the printf format can generate no more than cchWorstCase chars.
    // Writes up to cchWorstCase plus NUL.
    void
    WritePrintf(
        size_t cchWorstCase,
        _Printf_format_string_ char const* format,
        ...) noexcept _Printf_format_func_(3, 4)
    {
        WriteBegin(cchWorstCase);

        va_list args;
        va_start(args, format);
        unsigned const cchNeeded = vsnprintf(m_pDest, cchWorstCase + 1, format, args);
        va_end(args);

        assert(cchNeeded <= cchWorstCase);
        m_pDest += (cchNeeded <= cchWorstCase ? cchNeeded : cchWorstCase);
        WriteEnd();
    }

    // Requires: there is room for valSize * 3 - 1 chars.
    // Requires: valSize != 0.
    // Writes: valSize * 3 - 1, e.g. [00 11 22].
    void
    WriteHexBytes(void const* val, size_t valSize) noexcept
    {
        assert(valSize != 0);
        WriteBegin(valSize * 3 - 1);

        auto const pbVal = static_cast<uint8_t const*>(val);
        WriteHexByte(pbVal[0]);
        for (size_t i = 1; i != valSize; i += 1)
        {
            *m_pDest++ = ' ';
            WriteHexByte(pbVal[i]);
        }

        WriteEnd();
    }

    // Requires: there is room for 15 chars.
    // Writes 7..15 chars, e.g. [0.0.0.0] or [255.255.255.255].
    void
    WriteIPv4(uint32_t val) noexcept
    {
        auto constexpr DestWriteMax = 15u;
        WriteBegin(DestWriteMax);

#if _WIN32
        auto const p = reinterpret_cast<uint8_t const*>(&val);
        WritePrintf(DestWriteMax, "%u.%u.%u.%u",
            p[0], p[1], p[2], p[3]);
#else // _WIN32
        // INET_ADDRSTRLEN includes 1 nul.
        static_assert(INET_ADDRSTRLEN - 1 == DestWriteMax, "WriteIPv4Val length");
        inet_ntop(AF_INET, &val, m_pDest, DestWriteMax + 1);
        m_pDest += strnlen(m_pDest, DestWriteMax);
#endif // _WIN32

        WriteEnd();
    }

    // Requires: there is room for 45 chars.
    // Reads 16 bytes.
    // Writes 15..45 chars, e.g. [0:0:0:0:0:0:0:0] or [ffff:ffff:ffff:ffff:ffff:ffff:255.255.255.255].
    void
    WriteIPv6(void const* val) noexcept
    {
        auto constexpr DestWriteMax = 45u;
        WriteBegin(DestWriteMax);

#if _WIN32
        auto const p = static_cast<uint16_t const*>(val);
        WritePrintf(DestWriteMax,
            "%x:%x:%x:%x:%x:%x:%x:%x",
            bswap_16(p[0]), bswap_16(p[1]), bswap_16(p[2]), bswap_16(p[3]),
            bswap_16(p[4]), bswap_16(p[5]), bswap_16(p[6]), bswap_16(p[7]));
#else // _WIN32
        // INET6_ADDRSTRLEN includes 1 nul.
        static_assert(INET6_ADDRSTRLEN - 1 == DestWriteMax, "WriteIPv6Val length");
        inet_ntop(AF_INET6, val, m_pDest, DestWriteMax + 1);
        m_pDest += strnlen(m_pDest, DestWriteMax);
#endif // _WIN32

        WriteEnd();
    }

    // Requires: there is room for 36 chars.
    // Reads 16 bytes.
    // Writes 36 chars, e.g. [00000000-0000-0000-0000-000000000000].
    void
    WriteUuid(void const* val) noexcept
    {
        WriteBegin(36);

        uint8_t const* const pVal = static_cast<uint8_t const*>(val);
        WriteHexByte(pVal[0]);
        WriteHexByte(pVal[1]);
        WriteHexByte(pVal[2]);
        WriteHexByte(pVal[3]);
        *m_pDest++ = '-';
        WriteHexByte(pVal[4]);
        WriteHexByte(pVal[5]);
        *m_pDest++ = '-';
        WriteHexByte(pVal[6]);
        WriteHexByte(pVal[7]);
        *m_pDest++ = '-';
        WriteHexByte(pVal[8]);
        WriteHexByte(pVal[9]);
        *m_pDest++ = '-';
        WriteHexByte(pVal[10]);
        WriteHexByte(pVal[11]);
        WriteHexByte(pVal[12]);
        WriteHexByte(pVal[13]);
        WriteHexByte(pVal[14]);
        WriteHexByte(pVal[15]);

        WriteEnd();
    }

    // Requires: there is room for 26 chars.
    // Writes 19..26 chars, e.g. [2022-01-01T01:01:01] or [TIME(18000000000000000000)].
    void
    WriteDateTime(int64_t val) noexcept
    {
        auto const DestWriteMax = 26u;
        WriteBegin(DestWriteMax);

#if _WIN32
        if (-11644473600 <= val && val <= 910692730085)
        {
            int64_t ft = (val + 11644473600) * 10000000;
            SYSTEMTIME st;
            FileTimeToSystemTime(reinterpret_cast<FILETIME const*>(&ft), &st);
            WritePrintf(DestWriteMax, "%04u-%02u-%02uT%02u:%02u:%02u",
                st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);
        }
#else // _WIN32
        struct tm tm;
        if (gmtime_r(&val, &tm))
        {
            WritePrintf(DestWriteMax, "%04d-%02u-%02uT%02u:%02u:%02u",
                1900 + tm.tm_year, 1 + tm.tm_mon, tm.tm_mday,
                tm.tm_hour, tm.tm_min, tm.tm_sec);
        }
#endif // _WIN32
        else
        {
            WritePrintf(DestWriteMax, "TIME(%" PRId64 ")", val);
        }

        WriteEnd();
    }

    // Requires: there is room for 20 chars.
    // Writes 6..20 chars, e.g. [EIO(5)] or [ENOTRECOVERABLE(131)].
    void
    WriteErrno(uint32_t val) noexcept
    {
        auto const DestWriteMax = 20u;

        char const* str;
        switch (val)
        {
        default: str = "ERRNO"; break;
        case 1: str = "EPERM"; break;
        case 2: str = "ENOENT"; break;
        case 3: str = "ESRCH"; break;
        case 4: str = "EINTR"; break;
        case 5: str = "EIO"; break;
        case 6: str = "ENXIO"; break;
        case 7: str = "E2BIG"; break;
        case 8: str = "ENOEXEC"; break;
        case 9: str = "EBADF"; break;
        case 10: str = "ECHILD"; break;
        case 11: str = "EAGAIN"; break;
        case 12: str = "ENOMEM"; break;
        case 13: str = "EACCES"; break;
        case 14: str = "EFAULT"; break;
        case 15: str = "ENOTBLK"; break;
        case 16: str = "EBUSY"; break;
        case 17: str = "EEXIST"; break;
        case 18: str = "EXDEV"; break;
        case 19: str = "ENODEV"; break;
        case 20: str = "ENOTDIR"; break;
        case 21: str = "EISDIR"; break;
        case 22: str = "EINVAL"; break;
        case 23: str = "ENFILE"; break;
        case 24: str = "EMFILE"; break;
        case 25: str = "ENOTTY"; break;
        case 26: str = "ETXTBSY"; break;
        case 27: str = "EFBIG"; break;
        case 28: str = "ENOSPC"; break;
        case 29: str = "ESPIPE"; break;
        case 30: str = "EROFS"; break;
        case 31: str = "EMLINK"; break;
        case 32: str = "EPIPE"; break;
        case 33: str = "EDOM"; break;
        case 34: str = "ERANGE"; break;
        case 35: str = "EDEADLK"; break;
        case 36: str = "ENAMETOOLONG"; break;
        case 37: str = "ENOLCK"; break;
        case 38: str = "ENOSYS"; break;
        case 39: str = "ENOTEMPTY"; break;
        case 40: str = "ELOOP"; break;
        case 42: str = "ENOMSG"; break;
        case 43: str = "EIDRM"; break;
        case 44: str = "ECHRNG"; break;
        case 45: str = "EL2NSYNC"; break;
        case 46: str = "EL3HLT"; break;
        case 47: str = "EL3RST"; break;
        case 48: str = "ELNRNG"; break;
        case 49: str = "EUNATCH"; break;
        case 50: str = "ENOCSI"; break;
        case 51: str = "EL2HLT"; break;
        case 52: str = "EBADE"; break;
        case 53: str = "EBADR"; break;
        case 54: str = "EXFULL"; break;
        case 55: str = "ENOANO"; break;
        case 56: str = "EBADRQC"; break;
        case 57: str = "EBADSLT"; break;
        case 59: str = "EBFONT"; break;
        case 60: str = "ENOSTR"; break;
        case 61: str = "ENODATA"; break;
        case 62: str = "ETIME"; break;
        case 63: str = "ENOSR"; break;
        case 64: str = "ENONET"; break;
        case 65: str = "ENOPKG"; break;
        case 66: str = "EREMOTE"; break;
        case 67: str = "ENOLINK"; break;
        case 68: str = "EADV"; break;
        case 69: str = "ESRMNT"; break;
        case 70: str = "ECOMM"; break;
        case 71: str = "EPROTO"; break;
        case 72: str = "EMULTIHOP"; break;
        case 73: str = "EDOTDOT"; break;
        case 74: str = "EBADMSG"; break;
        case 75: str = "EOVERFLOW"; break;
        case 76: str = "ENOTUNIQ"; break;
        case 77: str = "EBADFD"; break;
        case 78: str = "EREMCHG"; break;
        case 79: str = "ELIBACC"; break;
        case 80: str = "ELIBBAD"; break;
        case 81: str = "ELIBSCN"; break;
        case 82: str = "ELIBMAX"; break;
        case 83: str = "ELIBEXEC"; break;
        case 84: str = "EILSEQ"; break;
        case 85: str = "ERESTART"; break;
        case 86: str = "ESTRPIPE"; break;
        case 87: str = "EUSERS"; break;
        case 88: str = "ENOTSOCK"; break;
        case 89: str = "EDESTADDRREQ"; break;
        case 90: str = "EMSGSIZE"; break;
        case 91: str = "EPROTOTYPE"; break;
        case 92: str = "ENOPROTOOPT"; break;
        case 93: str = "EPROTONOSUPPORT"; break;
        case 94: str = "ESOCKTNOSUPPORT"; break;
        case 95: str = "EOPNOTSUPP"; break;
        case 96: str = "EPFNOSUPPORT"; break;
        case 97: str = "EAFNOSUPPORT"; break;
        case 98: str = "EADDRINUSE"; break;
        case 99: str = "EADDRNOTAVAIL"; break;
        case 100: str = "ENETDOWN"; break;
        case 101: str = "ENETUNREACH"; break;
        case 102: str = "ENETRESET"; break;
        case 103: str = "ECONNABORTED"; break;
        case 104: str = "ECONNRESET"; break;
        case 105: str = "ENOBUFS"; break;
        case 106: str = "EISCONN"; break;
        case 107: str = "ENOTCONN"; break;
        case 108: str = "ESHUTDOWN"; break;
        case 109: str = "ETOOMANYREFS"; break;
        case 110: str = "ETIMEDOUT"; break;
        case 111: str = "ECONNREFUSED"; break;
        case 112: str = "EHOSTDOWN"; break;
        case 113: str = "EHOSTUNREACH"; break;
        case 114: str = "EALREADY"; break;
        case 115: str = "EINPROGRESS"; break;
        case 116: str = "ESTALE"; break;
        case 117: str = "EUCLEAN"; break;
        case 118: str = "ENOTNAM"; break;
        case 119: str = "ENAVAIL"; break;
        case 120: str = "EISNAM"; break;
        case 121: str = "EREMOTEIO"; break;
        case 122: str = "EDQUOT"; break;
        case 123: str = "ENOMEDIUM"; break;
        case 124: str = "EMEDIUMTYPE"; break;
        case 125: str = "ECANCELED"; break;
        case 126: str = "ENOKEY"; break;
        case 127: str = "EKEYEXPIRED"; break;
        case 128: str = "EKEYREVOKED"; break;
        case 129: str = "EKEYREJECTED"; break;
        case 130: str = "EOWNERDEAD"; break;
        case 131: str = "ENOTRECOVERABLE"; break;
        case 132: str = "ERFKILL"; break;
        case 133: str = "EHWPOISON"; break;
        }

        WritePrintf(DestWriteMax, "%s(%d)", str, val);
    }

    // Requires: there is room for 11 chars.
    // Writes 1..11 chars, e.g. [false] or [-2000000000].
    void
    WriteBoolean(int32_t boolVal) noexcept
    {
        auto const DestWriteMax = 11u;
        WriteBegin(DestWriteMax);

        switch (boolVal)
        {
        case 0:
            WriteUtf8("false"sv);
            break;
        case 1:
            WriteUtf8("true"sv);
            break;
        default:
            WritePrintf(DestWriteMax, "%d", boolVal);
            break;
        }

        WriteEnd();
    }

    // Returns true for ASCII control chars, double-quote, and backslash.
    static constexpr bool
    NeedsJsonEscape(uint8_t utf8Byte) noexcept
    {
        return utf8Byte < ' ' || utf8Byte == '"' || utf8Byte == '\\';
    }

    bool
    WantJsonSpace() const noexcept
    {
        return m_wantJsonSpace;
    }

    bool
    NeedJsonComma() const noexcept
    {
        return m_needJsonComma;
    }

    void
    SetNeedJsonComma(bool value) noexcept
    {
        m_needJsonComma = value;
    }

    // Requires: there is room for 1 char.
    // WriteUtf8Byte('['); SetNeedJsonComma(false);
    void
    WriteJsonArrayBegin() noexcept
    {
        assert(m_pDest < m_pDestEnd);
        *m_pDest++ = '[';
        m_needJsonComma = false;
    }

    // Requires: there is room for 1 char.
    // WriteUtf8Byte(']'); SetNeedJsonComma(true);
    void
    WriteJsonArrayEnd() noexcept
    {
        assert(m_pDest < m_pDestEnd);
        *m_pDest++ = ']';
        m_needJsonComma = true;
    }

    // Requires: there is room for 1 char.
    // WriteUtf8Byte('{'); SetNeedJsonComma(false);
    void
    WriteJsonStructBegin() noexcept
    {
        assert(m_pDest < m_pDestEnd);
        *m_pDest++ = '{';
        m_needJsonComma = false;
    }

    // Requires: there is room for 1 char.
    // WriteUtf8Byte('}'); SetNeedJsonComma(true);
    void
    WriteJsonStructEnd() noexcept
    {
        assert(m_pDest < m_pDestEnd);
        *m_pDest++ = '}';
        m_needJsonComma = true;
    }

    // Requires: there is room for 1 char.
    // Writes 0..1 chars, [] or [ ].
    void
    WriteJsonSpaceIfWanted() noexcept
    {
        assert(m_pDest < m_pDestEnd);
        *m_pDest = ' ';
        m_pDest += m_wantJsonSpace;
    }

    // Requires: there is room for 2 chars.
    // If NeedJsonComma, writes ','.
    // If WantJsonSpace, writes ' '.
    // Sets NeedJsonComma = true.
    void
    WriteJsonCommaSpaceAsNeeded() noexcept
    {
        WriteBegin(2);
        *m_pDest = ',';
        m_pDest += m_needJsonComma;
        *m_pDest = ' ';
        m_pDest += m_wantJsonSpace;
        m_needJsonComma = true;
        WriteEnd();
    }

    // Requires: there is room for 6 chars.
    // Requires: NeedsJsonEscape(ch).
    // Writes e.g. [\\u00ff].
    void
    WriteJsonEscapeChar(uint8_t utf8Byte) noexcept
    {
        WriteBegin(6);
        assert(utf8Byte < 0x80);

        *m_pDest++ = '\\';
        switch (utf8Byte)
        {
        case '\\': *m_pDest++ = '\\'; break;
        case '\"': *m_pDest++ = '"'; break;
        case '\b': *m_pDest++ = 'b'; break;
        case '\f': *m_pDest++ = 'f'; break;
        case '\n': *m_pDest++ = 'n'; break;
        case '\r': *m_pDest++ = 'r'; break;
        case '\t': *m_pDest++ = 't'; break;
        default:
            *m_pDest++ = 'u';
            *m_pDest++ = '0';
            *m_pDest++ = '0';
            WriteHexByte(utf8Byte);
            break;
        }

        WriteEnd();
    }

    // Requires: there is room for 7 chars.
    // Writes: 1..7 UTF-8 bytes, e.g. [a] or [\\u00ff].
    void
    WriteUcsCharJsonEscaped(uint32_t ucs4) noexcept
    {
        WriteBegin(7);
        if (ucs4 >= 0x80)
        {
            WriteUcsNonAsciiChar(ucs4);
        }
        else
        {
            char const ascii = static_cast<uint8_t>(ucs4);
            if (NeedsJsonEscape(ascii))
            {
                WriteJsonEscapeChar(ascii);
            }
            else
            {
                *m_pDest++ = ascii;
            }
        }
        WriteEnd();
    }

private:

    void
    AssertInvariants() noexcept
    {
        assert(m_pDest >= m_dest.data() + m_destCommitSize);
        assert(m_pDest <= m_dest.data() + m_dest.size());
        assert(m_pDestEnd == m_dest.data() + m_dest.size());
        assert(m_destCommitSize <= m_dest.size());
    }

    void
    GrowRoom(size_t roomNeeded)
    {
        AssertInvariants();
        size_t const curSize = m_pDest - m_dest.data();
        size_t const totalSize = curSize + roomNeeded;
        size_t const newSize = totalSize < roomNeeded // Did it overflow?
            ? ~static_cast<size_t>(0) // Yes: trigger exception from resize.
            : totalSize;
        assert(m_dest.size() < newSize);
        m_dest.resize(newSize);
        m_pDest = m_dest.data() + curSize;
        m_pDestEnd = m_dest.data() + m_dest.size();
        AssertInvariants();
    }
};

// Requires: there is room for 9 chars.
static void
WriteUcsVal(
    StringBuilder& sb,
    uint32_t ucs4,
    bool json) noexcept
{
    assert(9u <= sb.Room());
    if (json)
    {
        sb.WriteUtf8Byte('"');
        sb.WriteUcsCharJsonEscaped(ucs4); // UCS1
        sb.WriteUtf8Byte('"');
    }
    else
    {
        sb.WriteUcsChar(ucs4); // UCS1
    }
}

static void
AppendUtf8(
    StringBuilder& sb,
    std::string_view utf8)
{
    sb.EnsureRoom(utf8.size());
    sb.WriteUtf8(utf8);
}

static void
AppendUtf8JsonEscapedWithRoomReserved(
    StringBuilder& sb,
    std::string_view utf8,
    size_t roomReserved)
{
    assert(roomReserved <= sb.Room());
    assert(roomReserved >= utf8.size());
    auto roomNeededForJsonEscape = roomReserved + 5;
    for (auto utf8Byte : utf8)
    {
        if (sb.NeedsJsonEscape(utf8Byte))
        {
            sb.EnsureRoom(roomNeededForJsonEscape);
            sb.WriteJsonEscapeChar(utf8Byte);
        }
        else
        {
            sb.WriteUtf8Byte(utf8Byte);
        }

        roomNeededForJsonEscape -= 1;
    }
}

static void
AppendUtf8JsonEscaped(
    StringBuilder& sb,
    std::string_view utf8,
    size_t extraRoomNeeded)
{
    size_t const roomNeeded = utf8.size() + extraRoomNeeded;
    sb.EnsureRoom(roomNeeded);
    AppendUtf8JsonEscapedWithRoomReserved(sb, utf8, roomNeeded);
    assert(sb.Room() >= extraRoomNeeded);
}

static void
AppendUtf8Val(
    StringBuilder& sb,
    std::string_view utf8,
    bool json)
{
    if (json)
    {
        sb.EnsureRoom(utf8.size() + 2);
        sb.WriteUtf8Byte('"');
        AppendUtf8JsonEscaped(sb, utf8, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
    }
    else
    {
        AppendUtf8(sb, utf8);
    }
}

template<class Swapper, class CH>
static void
AppendUcs(
    StringBuilder& sb,
    CH const* pchUcs,
    size_t cchUcs)
{
    Swapper swapper;
    auto const pchEnd = pchUcs + cchUcs;
    sb.EnsureRoom(pchEnd - pchUcs);
    for (auto pch = pchUcs; pch != pchEnd; pch += 1)
    {
        uint32_t ucs4 = swapper(*pch);
        if (ucs4 >= 0x80)
        {
            sb.EnsureRoom(pchEnd - pch + 6);
            sb.WriteUcsNonAsciiChar(ucs4);
        }
        else
        {
            sb.WriteUtf8Byte(static_cast<uint8_t>(ucs4));
        }
    }
}

// Guaranteed to reserve at least one byte more than necessary.
template<class Swapper, class CH>
static void
AppendUcsJsonEscaped(
    StringBuilder& sb,
    CH const* pchUcs,
    size_t cchUcs,
    size_t extraRoomNeeded)
{
    Swapper swapper;
    auto const pchEnd = pchUcs + cchUcs;
    sb.EnsureRoom(pchEnd - pchUcs + extraRoomNeeded);
    for (auto pch = pchUcs; pch != pchEnd; pch += 1)
    {
        uint32_t ucs4 = swapper(*pch);
        if (ucs4 >= 0x80)
        {
            sb.EnsureRoom(pchEnd - pch + extraRoomNeeded + 6);
            sb.WriteUcsNonAsciiChar(ucs4);
        }
        else if (auto const ascii = static_cast<uint8_t>(ucs4);
            sb.NeedsJsonEscape(ascii))
        {
            sb.EnsureRoom(pchEnd - pch + extraRoomNeeded + 5);
            sb.WriteJsonEscapeChar(ascii);
        }
        else
        {
            sb.WriteUtf8Byte(ascii);
        }
    }
}

template<class Swapper, class CH>
static void
AppendUcsVal(
    StringBuilder& sb,
    CH const* pchUcs,
    size_t cchUcs,
    bool json)
{
    if (json)
    {
        sb.EnsureRoom(cchUcs + 2);
        sb.WriteUtf8Byte('"');
        AppendUcsJsonEscaped<Swapper>(sb, pchUcs, cchUcs, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
    }
    else
    {
        AppendUcs<Swapper>(sb, pchUcs, cchUcs);
    }
}

template<class Swapper>
static void
AppendUtf16Val(
    StringBuilder& sb,
    uint16_t const* pchUtf,
    size_t cchUtf,
    bool json)
{
    if (json)
    {
        sb.EnsureRoom(cchUtf + 2);
        sb.WriteUtf8Byte('"');
        AppendUcsJsonEscaped<Swapper>(sb, pchUtf, cchUtf, 1); // TODO: Surrogates
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
    }
    else
    {
        AppendUcs<Swapper>(sb, pchUtf, cchUtf); // TODO: Surrogates
    }
}

static bool
TryAppendUtfBomVal(
    StringBuilder& sb,
    void const* pUtf,
    size_t cbUtf,
    bool json)
{
    static uint32_t const Bom32SwapNo = 0x0000FEFF;
    static uint32_t const Bom32SwapYes = 0xFFFE0000;
    static uint16_t const Bom16SwapNo = 0xFEFF;
    static uint16_t const Bom16SwapYes = 0xFFFE;
    static uint8_t const Bom8[] = { 0xEF, 0xBB, 0xBF };

    bool matchedBom;
    if (cbUtf >= 4 && 0 == memcmp(pUtf, &Bom32SwapNo, 4))
    {
        AppendUcsVal<SwapNo>(sb,
            static_cast<uint32_t const*>(pUtf) + 1,
            cbUtf / sizeof(uint32_t) - 1,
            json);
        matchedBom = true;
    }
    else if (cbUtf >= 4 && 0 == memcmp(pUtf, &Bom32SwapYes, 4))
    {
        AppendUcsVal<SwapYes>(sb,
            static_cast<uint32_t const*>(pUtf) + 1,
            cbUtf / sizeof(uint32_t) - 1,
            json);
        matchedBom = true;
    }
    else if (cbUtf >= 2 && 0 == memcmp(pUtf, &Bom16SwapNo, 2))
    {
        AppendUtf16Val<SwapNo>(sb,
            static_cast<uint16_t const*>(pUtf) + 1,
            cbUtf / sizeof(uint16_t) - 1,
            json);
        matchedBom = true;
    }
    else if (cbUtf >= 2 && 0 == memcmp(pUtf, &Bom16SwapYes, 2))
    {
        AppendUtf16Val<SwapYes>(sb,
            static_cast<uint16_t const*>(pUtf) + 1,
            cbUtf / sizeof(uint16_t) - 1,
            json);
        matchedBom = true;
    }
    else if (cbUtf >= 3 && 0 == memcmp(pUtf, Bom8, 3))
    {
        AppendUtf8Val(sb,
            { static_cast<char const*>(pUtf) + 3, cbUtf - 3 },
            json);
        matchedBom = true;
    }
    else
    {
        matchedBom = false;
    }

    return matchedBom;
}

static void
AppendHexBytesVal(
    StringBuilder& sb,
    void const* val,
    size_t valSize,
    bool json)
{
    size_t const roomNeeded = (valSize * 3) + (json * 2u);
    sb.EnsureRoom(roomNeeded);

    sb.WriteQuoteIf(json);
    if (valSize != 0)
    {
        sb.WriteHexBytes(val, valSize);
    }
    sb.WriteQuoteIf(json);
}

// e.g. [, ].
static void
AppendJsonValueBegin(
    StringBuilder& sb,
    size_t extraRoomNeeded)
{
    sb.EnsureRoom(2 + extraRoomNeeded);
    sb.WriteJsonCommaSpaceAsNeeded();
}

// e.g. [, "abc": ].
static void
AppendJsonMemberBegin(
    StringBuilder& sb,
    uint16_t fieldTag,
    std::string_view nameUtf8,
    size_t extraRoomNeeded)
{
    unsigned const MemberNeeded = 17; // [, ";tag=0xFFFF": ]
    size_t roomNeeded = MemberNeeded + nameUtf8.size() + extraRoomNeeded;
    sb.EnsureRoom(roomNeeded);

    sb.WriteJsonCommaSpaceAsNeeded();
    sb.WriteUtf8Byte('"');

    AppendUtf8JsonEscapedWithRoomReserved(sb, nameUtf8, roomNeeded - 3);

    if (fieldTag != 0 && sb.WantFieldTag())
    {
        sb.WritePrintf(11, ";tag=0x%X", fieldTag);
    }

    sb.WriteUtf8Byte('"');
    sb.WriteUtf8Byte(':');
    sb.WriteJsonSpaceIfWanted();

    assert(sb.Room() >= extraRoomNeeded);
}

[[nodiscard]] static int
AppendValueImpl(
    StringBuilder& sb,
    void const* valData,
    size_t valSize,
    event_field_encoding encoding,
    event_field_format format,
    bool needsByteSwap,
    bool json)
{
    int err;

    switch (encoding)
    {
    default:
        err = ENOTSUP;
        break;
    case event_field_encoding_invalid:
    case event_field_encoding_struct:
        err = EINVAL;
        break;
    case event_field_encoding_value8:
        if (valSize != 1)
        {
            err = EINVAL;
        }
        else
        {
            unsigned const RoomNeeded = 11;
            sb.EnsureRoom(RoomNeeded);
            auto const val = *static_cast<uint8_t const*>(valData);
            switch (format)
            {
            default:
            case event_field_format_unsigned_int:
                // [255] = 3
                sb.WritePrintf(RoomNeeded, "%u", val);
                break;
            case event_field_format_signed_int:
                // [-128] = 4
                sb.WritePrintf(RoomNeeded, "%d", static_cast<int8_t>(val));
                break;
            case event_field_format_hex_int:
                // ["0xFF"] = 6
                sb.WriteQuoteIf(json);
                sb.WritePrintf(RoomNeeded - 2, "0x%X", val);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_boolean:
                // [-2000000000] = 11
                sb.WriteBoolean(val);
                break;
            case event_field_format_hex_bytes:
                // ["00"] = 4
                sb.WriteQuoteIf(json);
                sb.WriteHexBytes(valData, valSize);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_string8:
                // ["\u0000"] = 9
                WriteUcsVal(sb, val, json); // UCS1
                break;
            }

            err = 0;
        }
        break;
    case event_field_encoding_value16:
        if (valSize != 2)
        {
            err = EINVAL;
        }
        else
        {
            unsigned const RoomNeeded = 9;
            sb.EnsureRoom(RoomNeeded);
            auto const val = *static_cast<uint16_t const UNALIGNED*>(valData);
            switch (format)
            {
            default:
            case event_field_format_unsigned_int:
                // [65535] = 5
                sb.WritePrintf(RoomNeeded, "%u",
                    needsByteSwap ? bswap_16(val) : val);
                break;
            case event_field_format_signed_int:
                // [-32768] = 6
                sb.WritePrintf(RoomNeeded, "%d",
                    static_cast<int16_t>(needsByteSwap ? bswap_16(val) : val));
                break;
            case event_field_format_hex_int:
                // ["0xFFFF"] = 8
                sb.WriteQuoteIf(json);
                sb.WritePrintf(RoomNeeded - 2, "0x%X",
                    needsByteSwap ? bswap_16(val) : val);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_boolean:
                // [-32768] = 6
                sb.WriteBoolean(needsByteSwap ? bswap_16(val) : val);
                break;
            case event_field_format_hex_bytes:
                // ["00 00"] = 7
                sb.WriteQuoteIf(json);
                sb.WriteHexBytes(valData, valSize);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_string_utf:
                // ["\u0000"] = 9
                WriteUcsVal(sb, needsByteSwap ? bswap_16(val) : val, json); // UCS2
                break;
            case event_field_format_port:
                // [65535] = 5
                sb.WritePrintf(RoomNeeded, "%u",
                    be16toh(val));
                break;
            }

            err = 0;
        }
        break;
    case event_field_encoding_value32:
        if (valSize != 4)
        {
            err = EINVAL;
        }
        else
        {
            unsigned const RoomNeeded = 28;
            sb.EnsureRoom(RoomNeeded);
            auto const val = *static_cast<uint32_t const UNALIGNED*>(valData);
            switch (format)
            {
            default:
            case event_field_format_unsigned_int:
                // [4000000000] = 10
                sb.WritePrintf(RoomNeeded, "%u",
                    needsByteSwap ? bswap_32(val) : val);
                break;
            case event_field_format_signed_int:
            case event_field_format_pid:
                // [-2000000000] = 11
                sb.WritePrintf(RoomNeeded, "%d",
                    static_cast<int32_t>(needsByteSwap ? bswap_32(val) : val));
                break;
            case event_field_format_hex_int:
                // ["0xFFFFFFFF"] = 12
                sb.WriteQuoteIf(json);
                sb.WritePrintf(RoomNeeded - 2, "0x%X",
                    needsByteSwap ? bswap_32(val) : val);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_errno:
                // ["ENOTRECOVERABLE[131]"] = 22
                sb.WriteQuoteIf(json);
                sb.WriteErrno(needsByteSwap ? bswap_32(val) : val);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_time:
                // ["TIME(18000000000000000000)"] = 28
                sb.WriteQuoteIf(json);
                sb.WriteDateTime(static_cast<int32_t>(needsByteSwap ? bswap_32(val) : val));
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_boolean:
                // [-2000000000] = 11
                sb.WriteBoolean(needsByteSwap ? bswap_32(val) : val);
                break;
            case event_field_format_float:
            {
                // ["1.000000001"] = 13
                uint32_t const valSwapped = needsByteSwap ? bswap_32(val) : val;
                float valFloat;
                static_assert(sizeof(valFloat) == sizeof(valSwapped), "Expected 32-bit float");
                memcpy(&valFloat, &valSwapped, sizeof(valSwapped));
                bool const needQuote = json && !isfinite(valFloat);
                sb.WriteQuoteIf(needQuote);
                sb.WritePrintf(RoomNeeded - 2, "%.9g", valFloat);
                sb.WriteQuoteIf(needQuote);
                break;
            }
            case event_field_format_hex_bytes:
                // ["00 00 00 00"] = 13
                sb.WriteQuoteIf(json);
                sb.WriteHexBytes(valData, valSize);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_string_utf:
                // ["nnnnnnn"] = 9 (up to 7 utf-8 bytes)
                WriteUcsVal(sb, needsByteSwap ? bswap_32(val) : val, json); // UCS4
                break;
            case event_field_format_ipv4:
                // ["255.255.255.255"] = 17
                sb.WriteQuoteIf(json);
                sb.WriteIPv4(val);
                sb.WriteQuoteIf(json);
                break;
            }

            err = 0;
        }
        break;
    case event_field_encoding_value64:
        if (valSize != 8)
        {
            err = EINVAL;
        }
        else
        {
            unsigned const RoomNeeded = 28;
            sb.EnsureRoom(RoomNeeded);
            auto const val = *static_cast<uint64_t const UNALIGNED*>(valData);
            switch (format)
            {
            default:
            case event_field_format_unsigned_int:
                // [18000000000000000000] = 20
                sb.WritePrintf(RoomNeeded, "%" PRIu64,
                    needsByteSwap ? bswap_64(val) : val);
                break;
            case event_field_format_signed_int:
                // [-9000000000000000000] = 20
                sb.WritePrintf(RoomNeeded, "%" PRId64,
                    static_cast<int64_t>(needsByteSwap ? bswap_64(val) : val));
                break;
            case event_field_format_hex_int:
                // ["0xFFFFFFFFFFFFFFFF"] = 20
                sb.WriteQuoteIf(json);
                sb.WritePrintf(RoomNeeded - 2, "0x%" PRIX64,
                    needsByteSwap ? bswap_64(val) : val);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_time:
                // ["TIME(18000000000000000000)"] = 28
                sb.WriteQuoteIf(json);
                sb.WriteDateTime(static_cast<int64_t>(needsByteSwap ? bswap_64(val) : val));
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_float:
            {
                // ["1.00000000000000001"] = 21
                uint64_t const valSwapped = needsByteSwap ? bswap_64(val) : val;
                double valFloat;
                static_assert(sizeof(valFloat) == sizeof(valSwapped), "Expected 64-bit double");
                memcpy(&valFloat, &valSwapped, sizeof(valSwapped));
                bool const needQuote = json && !isfinite(valFloat);
                sb.WriteQuoteIf(needQuote);
                sb.WritePrintf(RoomNeeded - 2, "%.17g",
                    valFloat);
                sb.WriteQuoteIf(needQuote);
                break;
            }
            case event_field_format_hex_bytes:
                // ["00 00 00 00 00 00 00 00"] = 25
                sb.WriteQuoteIf(json);
                sb.WriteHexBytes(valData, valSize);
                sb.WriteQuoteIf(json);
                break;
            }

            err = 0;
        }
        break;
    case event_field_encoding_value128:
        if (valSize != 16)
        {
            err = EINVAL;
        }
        else
        {
            unsigned const RoomNeeded = 49;
            sb.EnsureRoom(RoomNeeded);
            switch (format)
            {
            default:
            case event_field_format_hex_bytes:
                // ["00 00 00 00 ... 00 00 00 00"] = 49
                sb.WriteQuoteIf(json);
                sb.WriteHexBytes(valData, valSize);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_uuid:
                // ["00000000-0000-0000-0000-000000000000"] = 38
                sb.WriteQuoteIf(json);
                sb.WriteUuid(valData);
                sb.WriteQuoteIf(json);
                break;
            case event_field_format_ipv6:
                // ["ffff:ffff:ffff:ffff:ffff:ffff:255.255.255.255"] = 47
                sb.WriteQuoteIf(json);
                sb.WriteIPv6(valData);
                sb.WriteQuoteIf(json);
                break;
            }

            err = 0;
        }
        break;
    case event_field_encoding_zstring_char8:
    case event_field_encoding_string_length16_char8:
        switch (format)
        {
        case event_field_format_hex_bytes:
            AppendHexBytesVal(sb, valData, valSize, json);
            break;
        case event_field_format_string8:
            AppendUcsVal<SwapNo>(sb,
                static_cast<uint8_t const*>(valData), valSize / sizeof(uint8_t),
                json);
            break;
        case event_field_format_string_utf_bom:
        case event_field_format_string_xml:
        case event_field_format_string_json:
            if (TryAppendUtfBomVal(sb, valData, valSize, json))
            {
                break;
            }
            __fallthrough;
        default:
        case event_field_format_string_utf:
            AppendUtf8Val(sb, { static_cast<char const*>(valData), valSize }, json);
            break;
        }

        err = 0;
        break;
    case event_field_encoding_zstring_char16:
    case event_field_encoding_string_length16_char16:
        if (valSize & 1)
        {
            err = EINVAL;
        }
        else
        {
            switch (format)
            {
            case event_field_format_hex_bytes:
                AppendHexBytesVal(sb, valData, valSize, json);
                break;
            case event_field_format_string_utf_bom:
            case event_field_format_string_xml:
            case event_field_format_string_json:
                if (TryAppendUtfBomVal(sb, valData, valSize, json))
                {
                    break;
                }
                __fallthrough;
            default:
            case event_field_format_string_utf:
                if (needsByteSwap)
                {
                    AppendUtf16Val<SwapYes>(sb,
                        static_cast<uint16_t const*>(valData), valSize / sizeof(uint16_t),
                        json);
                }
                else
                {
                    AppendUtf16Val<SwapNo>(sb,
                        static_cast<uint16_t const*>(valData), valSize / sizeof(uint16_t),
                        json);
                }
                break;
            }

            err = 0;
        }
        break;
    case event_field_encoding_zstring_char32:
    case event_field_encoding_string_length16_char32:
        if (valSize & 3)
        {
            err = EINVAL;
        }
        else
        {
            switch (format)
            {
            case event_field_format_hex_bytes:
                AppendHexBytesVal(sb, valData, valSize, json);
                break;
            case event_field_format_string_utf_bom:
            case event_field_format_string_xml:
            case event_field_format_string_json:
                if (TryAppendUtfBomVal(sb, valData, valSize, json))
                {
                    break;
                }
                __fallthrough;
            default:
            case event_field_format_string_utf:
                if (needsByteSwap)
                {
                    AppendUcsVal<SwapYes>(sb,
                        static_cast<uint32_t const*>(valData), valSize / sizeof(uint32_t),
                        json);
                }
                else
                {
                    AppendUcsVal<SwapNo>(sb,
                        static_cast<uint32_t const*>(valData), valSize / sizeof(uint32_t),
                        json);
                }
                break;
            }

            err = 0;
        }
        break;
    }

    return err;
}

[[nodiscard]] static int
AppendItemAsJsonImpl(
    StringBuilder& sb,
    EventEnumerator& enumerator,
    bool wantName)
{
    int err;
    int depth = 0;

    do
    {
        EventItemInfo itemInfo;

        switch (enumerator.State())
        {
        default:
            assert(!"Enumerator in invalid state.");
            err = EINVAL;
            goto Done;

        case EventEnumeratorState_BeforeFirstItem:
            depth += 1;
            break;

        case EventEnumeratorState_Value:

            itemInfo = enumerator.GetItemInfo();
            wantName && !itemInfo.ArrayFlags
                ? AppendJsonMemberBegin(sb, itemInfo.FieldTag, itemInfo.Name, 0)
                : AppendJsonValueBegin(sb, 0);
            err = AppendValueImpl(
                sb, itemInfo.ValueData, itemInfo.ValueSize,
                itemInfo.Encoding, itemInfo.Format, itemInfo.NeedByteSwap,
                true);
            if (err != 0)
            {
                goto Done;
            }
            break;

        case EventEnumeratorState_ArrayBegin:

            itemInfo = enumerator.GetItemInfo();
            wantName
                ? AppendJsonMemberBegin(sb, itemInfo.FieldTag, itemInfo.Name, 1)
                : AppendJsonValueBegin(sb, 1);
            sb.WriteJsonArrayBegin(); // 1 extra byte reserved above.

            depth += 1;
            break;

        case EventEnumeratorState_ArrayEnd:

            sb.EnsureRoom(2);
            sb.WriteJsonSpaceIfWanted();
            sb.WriteJsonArrayEnd();

            depth -= 1;
            break;

        case EventEnumeratorState_StructBegin:

            itemInfo = enumerator.GetItemInfo();
            wantName && !itemInfo.ArrayFlags
                ? AppendJsonMemberBegin(sb, itemInfo.FieldTag, itemInfo.Name, 1)
                : AppendJsonValueBegin(sb, 1);
            sb.WriteJsonStructBegin(); // 1 extra byte reserved above.

            depth += 1;
            break;

        case EventEnumeratorState_StructEnd:

            sb.EnsureRoom(2);
            sb.WriteJsonSpaceIfWanted();
            sb.WriteJsonStructEnd();

            depth -= 1;
            break;
        }

        wantName = true;
    } while (enumerator.MoveNext() && depth > 0);

    err = enumerator.LastError();

Done:

    return err;
}

static void
AppendMetaN(
    StringBuilder& sb,
    EventInfo const& ei)
{
    uint8_t cchName = 0;
    while (ei.Name[cchName] != '\0' && ei.Name[cchName] != ';')
    {
        cchName += 1;
    }

    AppendJsonMemberBegin(sb, 0, "n"sv, 1);
    sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
    AppendUtf8JsonEscaped(sb, { ei.TracepointName, ei.ProviderNameLength }, 1);
    sb.WriteUtf8Byte(':'); // 1 extra byte reserved above.
    AppendUtf8JsonEscaped(sb, { ei.Name, cchName }, 1);
    sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
}

static void
AppendMetaEventInfo(
    StringBuilder& sb,
    EventFormatterMetaFlags metaFlags,
    EventInfo const& ei)
{
    if (metaFlags & EventFormatterMetaFlags_provider)
    {
        AppendJsonMemberBegin(sb, 0, "provider"sv, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
        AppendUtf8JsonEscaped(sb, { ei.TracepointName, ei.ProviderNameLength }, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
    }

    if (metaFlags & EventFormatterMetaFlags_event)
    {
        AppendJsonMemberBegin(sb, 0, "event"sv, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
        AppendUtf8JsonEscaped(sb, ei.Name, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
    }

    if ((metaFlags & EventFormatterMetaFlags_id) && ei.Header.id != 0)
    {
        AppendJsonMemberBegin(sb, 0, "id"sv, 5);
        sb.WritePrintf(5, "%u", ei.Header.id);
    }

    if ((metaFlags & EventFormatterMetaFlags_version) && ei.Header.version != 0)
    {
        AppendJsonMemberBegin(sb, 0, "version"sv, 3);
        sb.WritePrintf(3, "%u", ei.Header.version);
    }

    if ((metaFlags & EventFormatterMetaFlags_level) && ei.Header.level != 0)
    {
        AppendJsonMemberBegin(sb, 0, "level"sv, 3);
        sb.WritePrintf(3, "%u", ei.Header.level);
    }

    if ((metaFlags & EventFormatterMetaFlags_keyword) && ei.Keyword != 0)
    {
        AppendJsonMemberBegin(sb, 0, "keyword"sv, 20);
        sb.WritePrintf(20, "\"0x%" PRIX64 "\"", ei.Keyword);
    }

    if ((metaFlags & EventFormatterMetaFlags_opcode) && ei.Header.opcode != 0)
    {
        AppendJsonMemberBegin(sb, 0, "opcode"sv, 3);
        sb.WritePrintf(3, "%u", ei.Header.opcode);
    }

    if ((metaFlags & EventFormatterMetaFlags_tag) && ei.Header.tag != 0)
    {
        AppendJsonMemberBegin(sb, 0, "tag"sv, 8);
        sb.WritePrintf(8, "\"0x%X\"", ei.Header.tag);
    }

    if ((metaFlags & EventFormatterMetaFlags_activity) && ei.ActivityId != nullptr)
    {
        AppendJsonMemberBegin(sb, 0, "activity"sv, 38);
        sb.WriteUtf8Byte('"');
        sb.WriteUuid(ei.ActivityId);
        sb.WriteUtf8Byte('"');
    }

    if ((metaFlags & EventFormatterMetaFlags_relatedActivity) && ei.RelatedActivityId != nullptr)
    {
        AppendJsonMemberBegin(sb, 0, "relatedActivity"sv, 38);
        sb.WriteUtf8Byte('"');
        sb.WriteUuid(ei.RelatedActivityId);
        sb.WriteUtf8Byte('"');
    }

    if ((metaFlags & EventFormatterMetaFlags_options) && ei.OptionsIndex < ei.TracepointNameLength)
    {
        AppendJsonMemberBegin(sb, 0, "options"sv, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
        std::string_view options = {
            ei.TracepointName + ei.OptionsIndex,
            (size_t)ei.TracepointNameLength - ei.OptionsIndex };
        AppendUtf8JsonEscaped(sb, options, 1);
        sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
    }

    if (metaFlags & EventFormatterMetaFlags_flags)
    {
        AppendJsonMemberBegin(sb, 0, "flags"sv, 6);
        sb.WritePrintf(6, "\"0x%X\"", ei.Header.flags);
    }
}

// Assumes that there is room for '[' when called.
static void
AppendIntegerSampleFieldAsJsonImpl(
    StringBuilder& sb,
    std::string_view fieldRawData,
    PerfFieldMetadata const& fieldMetadata,
    bool fileBigEndian,
    char const* format32,
    char const* format64)
{
    assert(sb.Room() > 0);
    PerfByteReader const byteReader(fileBigEndian);

    if (fieldMetadata.Array() == PerfFieldArrayNone)
    {
        switch (fieldMetadata.ElementSize())
        {
        case PerfFieldElementSize8:
            if (fieldRawData.size() < sizeof(uint8_t))
            {
                AppendUtf8(sb, "null"sv);
            }
            else
            {
                unsigned const RoomNeeded = 6; // ["0xFF"]
                sb.EnsureRoom(RoomNeeded);
                auto val = byteReader.ReadAsU8(fieldRawData.data());
                sb.WritePrintf(RoomNeeded, format32, val);
            }
            break;
        case PerfFieldElementSize16:
            if (fieldRawData.size() < sizeof(uint16_t))
            {
                AppendUtf8(sb, "null"sv);
            }
            else
            {
                unsigned const RoomNeeded = 8; // ["0xFFFF"]
                sb.EnsureRoom(RoomNeeded);
                auto val = byteReader.ReadAsU16(fieldRawData.data());
                sb.WritePrintf(RoomNeeded, format32, val);
            }
            break;
        case PerfFieldElementSize32:
            if (fieldRawData.size() < sizeof(uint32_t))
            {
                AppendUtf8(sb, "null"sv);
            }
            else
            {
                unsigned const RoomNeeded = 12; // ["0xFFFFFFFF"]
                sb.EnsureRoom(RoomNeeded);
                auto val = byteReader.ReadAsU32(fieldRawData.data());
                sb.WritePrintf(RoomNeeded, format32, val);
            }
            break;
        case PerfFieldElementSize64:
            if (fieldRawData.size() < sizeof(uint64_t))
            {
                AppendUtf8(sb, "null"sv);
            }
            else
            {
                unsigned const RoomNeeded = 20; // ["0xFFFFFFFFFFFFFFFF"]
                sb.EnsureRoom(RoomNeeded);
                auto val = byteReader.ReadAsU64(fieldRawData.data());
                sb.WritePrintf(RoomNeeded, format64, val);
            }
            break;
        }
    }
    else
    {
        void const* const pvData = fieldRawData.data();
        auto const cbData = fieldRawData.size();

        sb.WriteJsonArrayBegin(); // Caller is expected to give us room for 1 char.
        switch (fieldMetadata.ElementSize())
        {
        case PerfFieldElementSize8:
            for (auto p = static_cast<uint8_t const*>(pvData), pEnd = p + cbData / sizeof(p[0]); p != pEnd; p += 1)
            {
                unsigned const RoomNeeded = 6; // ["0xFF"]
                sb.EnsureRoom(RoomNeeded + 2);
                sb.WriteJsonCommaSpaceAsNeeded();
                sb.WritePrintf(RoomNeeded, format32, byteReader.Read(p));
            }
            break;
        case PerfFieldElementSize16:
            for (auto p = static_cast<uint16_t const*>(pvData), pEnd = p + cbData / sizeof(p[0]); p != pEnd; p += 1)
            {
                unsigned const RoomNeeded = 8; // ["0xFFFF"]
                sb.EnsureRoom(RoomNeeded + 2);
                sb.WriteJsonCommaSpaceAsNeeded();
                sb.WritePrintf(RoomNeeded, format32, byteReader.Read(p));
            }
            break;
        case PerfFieldElementSize32:
            for (auto p = static_cast<uint32_t const*>(pvData), pEnd = p + cbData / sizeof(p[0]); p != pEnd; p += 1)
            {
                unsigned const RoomNeeded = 12; // ["0xFFFFFFFF"]
                sb.EnsureRoom(RoomNeeded + 2);
                sb.WriteJsonCommaSpaceAsNeeded();
                sb.WritePrintf(RoomNeeded, format32, byteReader.Read(p));
            }
            break;
        case PerfFieldElementSize64:
            for (auto p = static_cast<uint64_t const*>(pvData), pEnd = p + cbData / sizeof(p[0]); p != pEnd; p += 1)
            {
                unsigned const RoomNeeded = 20; // ["0xFFFFFFFFFFFFFFFF"]
                sb.EnsureRoom(RoomNeeded + 2);
                sb.WriteJsonCommaSpaceAsNeeded();
                sb.WritePrintf(RoomNeeded, format64, byteReader.Read(p));
            }
            break;
        }

        sb.EnsureRoom(2);
        sb.WriteJsonSpaceIfWanted();
        sb.WriteJsonArrayEnd();
    }
}

static void
AppendSampleFieldAsJsonImpl(
    StringBuilder& sb,
    _In_reads_bytes_(fieldRawDataSize) void const* fieldRawData,
    size_t fieldRawDataSize,
    PerfFieldMetadata const& fieldMetadata,
    bool fileBigEndian,
    bool wantName)
{
    PerfByteReader const byteReader(fileBigEndian);
    auto const fieldRawDataChars = static_cast<char const*>(fieldRawData);

    // Note: AppendIntegerSampleFieldAsJsonImpl expects 1 byte reserved for '['.
    wantName
        ? AppendJsonMemberBegin(sb, 0, fieldMetadata.Name(), 1)
        : AppendJsonValueBegin(sb, 1);
    switch (fieldMetadata.Format())
    {
    default:
    case PerfFieldFormatNone:
        if (fieldMetadata.Array() == PerfFieldArrayNone ||
            fieldMetadata.ElementSize() == PerfFieldElementSize8)
        {
            // Single unknown item, or an array of 8-bit unknown items: Treat as one binary blob.
            AppendHexBytesVal(sb, fieldRawDataChars, fieldRawDataSize, true);
            break;
        }
        // Array of unknown items: Treat as hex integers.
        [[fallthrough]];
    case PerfFieldFormatHex:
        AppendIntegerSampleFieldAsJsonImpl(sb, { fieldRawDataChars, fieldRawDataSize },
            fieldMetadata, fileBigEndian, "\"0x%" PRIX32 "\"", "\"0x%" PRIX64 "\"");
        break;
    case PerfFieldFormatUnsigned:
        AppendIntegerSampleFieldAsJsonImpl(sb, { fieldRawDataChars, fieldRawDataSize },
            fieldMetadata, fileBigEndian, "%" PRIu32, "%" PRIu64);
        break;
    case PerfFieldFormatSigned:
        AppendIntegerSampleFieldAsJsonImpl(sb, { fieldRawDataChars, fieldRawDataSize },
            fieldMetadata, fileBigEndian, "%" PRId32, "%" PRId64);
        break;
    case PerfFieldFormatString:
        AppendUcsVal<SwapNo>(sb,
            reinterpret_cast<uint8_t const*>(fieldRawDataChars),
            strnlen(fieldRawDataChars, fieldRawDataSize),
            true);
        break;
    }
}

int
EventFormatter::AppendSampleAsJson(
    std::string& dest,
    PerfSampleEventInfo const& sampleEventInfo,
    bool fileBigEndian,
    EventFormatterJsonFlags jsonFlags,
    EventFormatterMetaFlags metaFlags,
    uint32_t moveNextLimit)
{
    StringBuilder sb(dest, jsonFlags);
    int err = 0;

    EventEnumerator enumerator;
    EventInfo eventInfo;
    bool eventInfoValid;
    std::string_view sampleEventName;
    std::string_view sampleProviderName;
    auto const sampleEventInfoSampleType = sampleEventInfo.SampleType();
    auto const sampleEventInfoMetadata = sampleEventInfo.Metadata();

    if (sampleEventInfoMetadata &&
        sampleEventInfoMetadata->Kind() == PerfEventKind::EventHeader)
    {
        // eventheader metadata.

        auto const& meta = *sampleEventInfoMetadata;
        auto const eventHeaderOffset = meta.Fields()[meta.CommonFieldCount()].Offset();
        if (eventHeaderOffset > sampleEventInfo.raw_data_size ||
            !enumerator.StartEvent(
                meta.Name().data(),
                meta.Name().size(),
                static_cast<char const*>(sampleEventInfo.raw_data) + eventHeaderOffset,
                sampleEventInfo.raw_data_size - eventHeaderOffset,
                moveNextLimit))
        {
            goto NotEventHeader;
        }

        eventInfo = enumerator.GetEventInfo();
        eventInfoValid = true;

        (jsonFlags & EventFormatterJsonFlags_Name)
            ? AppendJsonMemberBegin(sb, 0, eventInfo.Name, 1)
            : AppendJsonValueBegin(sb, 1);
        sb.WriteJsonStructBegin(); // top-level

        if (metaFlags & EventFormatterMetaFlags_n)
        {
            AppendMetaN(sb, eventInfo);
        }

        if (metaFlags & EventFormatterMetaFlags_common)
        {
            for (size_t iField = 0; iField != meta.CommonFieldCount(); iField += 1)
            {
                auto const fieldMeta = meta.Fields()[iField];
                auto const fieldData = fieldMeta.GetFieldBytes(
                    sampleEventInfo.raw_data,
                    sampleEventInfo.raw_data_size,
                    fileBigEndian);
                AppendSampleFieldAsJsonImpl(sb, fieldData.data(), fieldData.size(), fieldMeta, fileBigEndian, true);
            }
        }

        err = AppendItemAsJsonImpl(sb, enumerator, true);
        if (err != 0)
        {
            goto Done;
        }
    }
    else
    {
    NotEventHeader:

        auto const sampleEventInfoName = sampleEventInfo.Name();
        if (sampleEventInfoName[0] == 0 && sampleEventInfoMetadata != nullptr)
        {
            // No name from PERF_HEADER_EVENT_DESC, but metadata is present so use that.
            sampleProviderName = sampleEventInfoMetadata->SystemName();
            sampleEventName = sampleEventInfoMetadata->Name();
        }
        else
        {
            auto const sampleEventNameColon = strchr(sampleEventInfoName, ':');
            if (sampleEventNameColon == nullptr)
            {
                // No colon in name.
                // Put everything into provider name (probably "" anyway).
                sampleProviderName = sampleEventInfoName;
                sampleEventName = "";
            }
            else
            {
                // Name contained a colon.
                // Provider name is everything before colon, event name is everything after.
                sampleProviderName = { sampleEventInfoName, static_cast<size_t>(sampleEventNameColon - sampleEventInfoName) };
                sampleEventName = sampleEventNameColon + 1;
            }
        }

        PerfByteReader const byteReader(fileBigEndian);

        eventInfoValid = false;

        (jsonFlags & EventFormatterJsonFlags_Name)
            ? AppendJsonMemberBegin(sb, 0, sampleEventName, 1)
            : AppendJsonValueBegin(sb, 1);
        sb.WriteJsonStructBegin(); // top-level

        if (metaFlags & EventFormatterMetaFlags_n)
        {
            AppendJsonMemberBegin(sb, 0, "n"sv, 1);
            sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
            AppendUcsJsonEscaped<SwapNo>(sb,
                reinterpret_cast<uint8_t const*>(sampleProviderName.data()),
                sampleProviderName.size(),
                1);
            sb.WriteUtf8Byte(':'); // 1 extra byte reserved above.
            AppendUcsJsonEscaped<SwapNo>(sb,
                reinterpret_cast<uint8_t const*>(sampleEventName.data()),
                sampleEventName.size(),
                1);
            sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
        }

        if (sampleEventInfoMetadata)
        {
            auto const& meta = *sampleEventInfoMetadata;
            size_t const firstField = (metaFlags & EventFormatterMetaFlags_common)
                ? 0u
                : meta.CommonFieldCount();
            for (size_t iField = firstField; iField < meta.Fields().size(); iField += 1)
            {
                auto const fieldMeta = meta.Fields()[iField];
                auto const fieldData = fieldMeta.GetFieldBytes(
                    sampleEventInfo.raw_data,
                    sampleEventInfo.raw_data_size,
                    fileBigEndian);
                AppendSampleFieldAsJsonImpl(sb, fieldData.data(), fieldData.size(), fieldMeta, fileBigEndian, true);
            }
        }
        else if (sampleEventInfoSampleType & PERF_SAMPLE_RAW)
        {
            AppendJsonMemberBegin(sb, 0, "raw"sv, 0);
            AppendHexBytesVal(sb,
                sampleEventInfo.raw_data, sampleEventInfo.raw_data_size,
                true);
        }
    }

    if (0 != (metaFlags & ~EventFormatterMetaFlags_n))
    {
        AppendJsonMemberBegin(sb, 0, "meta"sv, 1);
        sb.WriteJsonStructBegin(); // meta

        if ((metaFlags & EventFormatterMetaFlags_time) && (sampleEventInfoSampleType & PERF_SAMPLE_TIME))
        {
            AppendJsonMemberBegin(sb, 0, "time"sv, 39); // "DATETIME.nnnnnnnnnZ" = 1 + 26 + 12
            if (sampleEventInfo.session_info->ClockOffsetKnown())
            {
                auto timeSpec = sampleEventInfo.session_info->TimeToRealTime(sampleEventInfo.time);
                sb.WriteUtf8Byte('\"');
                sb.WriteDateTime(timeSpec.tv_sec);
                sb.WritePrintf(12, ".%09uZ\"", timeSpec.tv_nsec);
            }
            else
            {
                sb.WritePrintf(22, "%" PRIu64 ".%09u",
                    sampleEventInfo.time / 1000000000,
                    static_cast<unsigned>(sampleEventInfo.time % 1000000000));
            }
        }

        if ((metaFlags & EventFormatterMetaFlags_cpu) && (sampleEventInfoSampleType & PERF_SAMPLE_CPU))
        {
            AppendJsonMemberBegin(sb, 0, "cpu"sv, 10);
            sb.WritePrintf(10, "%u", sampleEventInfo.cpu);
        }

        if ((metaFlags & EventFormatterMetaFlags_pid) && (sampleEventInfoSampleType & PERF_SAMPLE_TID))
        {
            AppendJsonMemberBegin(sb, 0, "pid"sv, 10);
            sb.WritePrintf(10, "%u", sampleEventInfo.pid);
        }

        if ((metaFlags & EventFormatterMetaFlags_tid) && (sampleEventInfoSampleType & PERF_SAMPLE_TID))
        {
            AppendJsonMemberBegin(sb, 0, "tid"sv, 10);
            sb.WritePrintf(10, "%u", sampleEventInfo.tid);
        }

        if (eventInfoValid)
        {
            AppendMetaEventInfo(sb, metaFlags, eventInfo);
        }
        else
        {
            if ((metaFlags & EventFormatterMetaFlags_provider) && !sampleProviderName.empty())
            {
                AppendJsonMemberBegin(sb, 0, "provider"sv, 1);
                sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
                AppendUtf8JsonEscaped(sb, sampleProviderName, 1);
                sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
            }

            if ((metaFlags & EventFormatterMetaFlags_event) && !sampleEventName.empty())
            {
                AppendJsonMemberBegin(sb, 0, "event"sv, 1);
                sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
                AppendUtf8JsonEscaped(sb, sampleEventName, 1);
                sb.WriteUtf8Byte('"'); // 1 extra byte reserved above.
            }

        }

        sb.EnsureRoom(4); // Room to end meta and top-level.
        sb.WriteJsonSpaceIfWanted();
        sb.WriteJsonStructEnd(); // meta
    }
    else
    {
        sb.EnsureRoom(2); // Room to end top-level.
    }

    sb.WriteJsonSpaceIfWanted();
    sb.WriteJsonStructEnd(); // top-level

Done:

    if (err == 0)
    {
        sb.Commit();
    }

    return err;
}

int
EventFormatter::AppendSampleFieldAsJson(
    std::string& dest,
    _In_reads_bytes_(fieldRawDataSize) void const* fieldRawData,
    size_t fieldRawDataSize,
    PerfFieldMetadata const& fieldMetadata,
    bool fileBigEndian,
    EventFormatterJsonFlags jsonFlags)
{
    StringBuilder sb(dest, jsonFlags);
    AppendSampleFieldAsJsonImpl(sb, fieldRawData, fieldRawDataSize, fieldMetadata, fileBigEndian,
        (jsonFlags & EventFormatterJsonFlags_Name));
    sb.Commit();
    return 0;
}

int
EventFormatter::AppendEventAsJsonAndMoveToEnd(
    std::string& dest,
    EventEnumerator& enumerator,
    EventFormatterJsonFlags jsonFlags,
    EventFormatterMetaFlags metaFlags)
{
    assert(EventEnumeratorState_BeforeFirstItem == enumerator.State());

    StringBuilder sb(dest, jsonFlags);
    auto const ei = enumerator.GetEventInfo();

    int err;

    (jsonFlags & EventFormatterJsonFlags_Name)
        ? AppendJsonMemberBegin(sb, 0, ei.Name, 1)
        : AppendJsonValueBegin(sb, 1);
    sb.WriteJsonStructBegin(); // top-level.

    if (metaFlags & EventFormatterMetaFlags_n)
    {
        AppendMetaN(sb, ei);
    }

    err = AppendItemAsJsonImpl(sb, enumerator, true);
    if (err == 0)
    {
        if (0 != (metaFlags & ~EventFormatterMetaFlags_n))
        {
            AppendJsonMemberBegin(sb, 0, "meta"sv, 1);
            sb.WriteJsonStructBegin(); // meta.

            AppendMetaEventInfo(sb, metaFlags, ei);

            sb.EnsureRoom(4); // Room to end meta and top-level.
            sb.WriteJsonSpaceIfWanted();
            sb.WriteJsonStructEnd(); // meta
        }
        else
        {
            sb.EnsureRoom(2); // Room to end top-level.
        }

        sb.WriteJsonSpaceIfWanted();
        sb.WriteJsonStructEnd(); // top-level
    }

    if (err == 0)
    {
        sb.Commit();
    }

    return err;
}

int
EventFormatter::AppendItemAsJsonAndMoveNextSibling(
    std::string& dest,
    EventEnumerator& enumerator,
    EventFormatterJsonFlags jsonFlags)
{
    StringBuilder sb(dest, jsonFlags);
    int const err = AppendItemAsJsonImpl(sb, enumerator, (jsonFlags & EventFormatterJsonFlags_Name));
    if (err == 0)
    {
        sb.Commit();
    }
    return err;
}

int
EventFormatter::AppendValue(
    std::string& dest,
    EventEnumerator const& enumerator)
{
    return AppendValue(dest, enumerator.GetItemInfo());
}

int
EventFormatter::AppendValue(
    std::string& dest,
    EventItemInfo const& valueItemInfo)
{
    return AppendValue(dest, valueItemInfo.ValueData, valueItemInfo.ValueSize,
        valueItemInfo.Encoding, valueItemInfo.Format, valueItemInfo.NeedByteSwap);
}

int
EventFormatter::AppendValue(
    std::string& dest,
    _In_reads_bytes_(valueSize) void const* valueData,
    uint32_t valueSize,
    event_field_encoding encoding,
    event_field_format format,
    bool needsByteSwap)
{
    StringBuilder sb(dest, EventFormatterJsonFlags_None);
    int const err = AppendValueImpl(sb, valueData, valueSize,
        encoding, format, needsByteSwap, false);
    if (err == 0)
    {
        sb.Commit();
    }
    return err;
}

void
EventFormatter::AppendUuid(
    std::string& dest,
    _In_reads_bytes_(16) uint8_t const* uuid)
{
    StringBuilder sb(dest, EventFormatterJsonFlags_None);
    sb.EnsureRoom(36);
    sb.WriteUuid(uuid);
    sb.Commit();
}
