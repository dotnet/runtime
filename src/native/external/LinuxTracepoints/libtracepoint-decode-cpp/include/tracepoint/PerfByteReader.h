// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once
#ifndef _included_PerfByteReader_h
#define _included_PerfByteReader_h

#include <stdint.h>
#include <string.h> // memcpy

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _In_
#define _In_
#endif
#ifndef _In_reads_bytes_
#define _In_reads_bytes_(cb)
#endif

namespace tracepoint_decode
{
    // Loads values from the perf event data buffer, handling misaligned
    // and byte-swapped values.
    class PerfByteReader
    {
        bool m_bigEndian;

    public:

        // Initializes a data reader that treats input as host-endian.
        //
        // Postcondition: this->ByteSwapNeeded() == false.
        PerfByteReader() noexcept;

        // If bigEndian is true, initializes a data reader that assumes input is
        // big-endian. Otherwise, assumes input is little-endian.
        //
        // Postcondition: this->BigEndian() == bigEndian.
        explicit
        PerfByteReader(bool bigEndian) noexcept;

        // Returns true if this reader is treating its input data as big-endian.
        // Returns false if this reader is treating its input data as little-endian.
        bool
        BigEndian() const noexcept;

        // Returns BigEndian() != HOST_IS_BIG_ENDIAN.
        bool
        ByteSwapNeeded() const noexcept;

        // Returns *pSrc as uint8_t.
        uint8_t
        ReadAsU8(_In_reads_bytes_(1) void const* pSrc) const noexcept;

        // Reads 2 bytes from pSrc, performs byteswap if appropriate,
        // then returns the result as uint16_t.
        uint16_t
        ReadAsU16(_In_reads_bytes_(2) void const* pSrc) const noexcept;

        // Reads 4 bytes from pSrc, performs byteswap if appropriate,
        // then returns the result as uint32_t.
        uint32_t
        ReadAsU32(_In_reads_bytes_(4) void const* pSrc) const noexcept;

        // Reads 8 bytes from pSrc, performs byteswap if appropriate,
        // then returns the result as uint64_t.
        uint64_t
        ReadAsU64(_In_reads_bytes_(8) void const* pSrc) const noexcept;

        // Requires: cbSrc is 1, 2, or 4.
        // Reads cbSrc bytes from pSrc, performs byteswap if appropriate,
        // then returns the result cast to uint32_t.
        uint32_t
        ReadAsDynU32(_In_reads_bytes_(cbSrc) void const* pSrc, uint8_t cbSrc) const noexcept;

        // Requires: cbSrc is 1, 2, 4, or 8.
        // Reads cbSrc bytes from pSrc, performs byteswap if appropriate,
        // then returns the result cast to uint64_t.
        uint64_t
        ReadAsDynU64(_In_reads_bytes_(cbSrc) void const* pSrc, uint8_t cbSrc) const noexcept;

        // Requires: sizeof(ValType) is 1, 2, 4, or 8.
        // Reads sizeof(ValType) bytes from pSrc, performs byteswap if appropriate,
        // then returns the result cast to ValType.
        // ValType should be a trivial type, e.g. uint32_t, long, or double.
        template<class ValType>
        ValType
        ReadAs(_In_reads_bytes_(sizeof(ValType)) void const* pSrc) const noexcept
        {
            if constexpr (sizeof(ValType) == sizeof(uint8_t))
            {
                ValType v;
                memcpy(&v, pSrc, sizeof(v));
                return v;
            }
            else if constexpr (sizeof(ValType) == sizeof(uint16_t))
            {
                auto const uintVal = ReadAsU16(pSrc);
                ValType v;
                memcpy(&v, &uintVal, sizeof(v));
                return v;
            }
            else if constexpr (sizeof(ValType) == sizeof(uint32_t))
            {
                auto const uintVal = ReadAsU32(pSrc);
                ValType v;
                memcpy(&v, &uintVal, sizeof(v));
                return v;
            }
            else if constexpr (sizeof(ValType) == sizeof(uint64_t))
            {
                auto const uintVal = ReadAsU64(pSrc);
                ValType v;
                memcpy(&v, &uintVal, sizeof(v));
                return v;
            }
            else
            {
                static_assert(sizeof(ValType) == 0,
                    "ReadAs supports values of size 1, 2, 4, and 8.");
            }
        }

        // Requires: sizeof(ValType) is 1, 2, 4, or 8.
        // Reads sizeof(ValType) bytes from pSrc, performs byteswap if appropriate,
        // then returns the result cast to ValType.
        // ValType should be a trivial type, e.g. uint32_t, long, or double.
        template<class ValType>
        ValType
        Read(_In_ ValType const* pSrc) const noexcept
        {
            return ReadAs<ValType>(pSrc);
        }
    };
}
// namespace tracepoint_decode

#endif // _included_PerfByteReader_h
