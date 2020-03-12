// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __READER_H__
#define __READER_H__

#include <cstdint>
#include "pal.h"

namespace bundle
{
    // Helper class for reading sequentially from the memory-mapped bundle file.
    struct reader_t
    {
        reader_t(const int8_t* base_ptr, int64_t bound, int64_t start_offset = 0)
            : m_base_ptr(base_ptr)
            , m_ptr(base_ptr)
            , m_bound(bound)
            , m_bound_ptr(add_without_overflow(base_ptr, bound))
        {
            set_offset(start_offset);
        }

    public:

        void set_offset(int64_t offset);

        operator const int8_t*() const
        {
            return m_ptr;
        }

        int8_t read()
        {
            bounds_check();
            return *m_ptr++;
        }

        // Copy len bytes from m_ptr to dest
        void read(void* dest, int64_t len)
        {
            bounds_check(len);
            memcpy(dest, m_ptr, len);
            m_ptr += len;
        }

        // Return a pointer to the requested bytes within the memory-mapped file.
        // Skip over len bytes.
        const int8_t* read_direct(int64_t len)
        {
            bounds_check(len);
            const int8_t *ptr = m_ptr;
            m_ptr += len;
            return ptr;
        }

        size_t read_path_length();
        size_t read_path_string(pal::string_t &str);

    private:

        void bounds_check(int64_t len = 1);
        static const int8_t* add_without_overflow(const int8_t* ptr, int64_t len);

        const int8_t* const m_base_ptr;
        const int8_t* m_ptr;
        const int64_t m_bound;
        const int8_t* const m_bound_ptr;
    };
}

#endif // __READER_H__
