// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __READER_H__
#define __READER_H__

#include <cstdint>
#include "pal.h"
#include "utils.h"

namespace bundle
{
    // Helper class for reading sequentially from the memory-mapped bundle file.
    struct reader_t
    {
        reader_t(const char* base_ptr, int64_t bound, int64_t start_offset = 0)
            : m_base_ptr(base_ptr)
            , m_ptr(base_ptr)
            , m_bound(bound)
            , m_bound_ptr(add_without_overflow(base_ptr, bound))
        {
            set_offset(start_offset);
        }

    public:

        void set_offset(int64_t offset);

        operator const char*() const
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
            memcpy(dest, m_ptr, to_size_t_dbgchecked(len));
            m_ptr += len;
        }

        // Return a pointer to the requested bytes within the memory-mapped file.
        // Skip over len bytes.
        const char* read_direct(int64_t len)
        {
            bounds_check(len);
            const char *ptr = m_ptr;
            m_ptr += len;
            return ptr;
        }

        size_t read_path_length();
        size_t read_path_string(pal::string_t &str);

    private:

        void bounds_check(int64_t len = 1);
        static const char* add_without_overflow(const char* ptr, int64_t len);

        const char* const m_base_ptr;
        const char* m_ptr;
        const int64_t m_bound;
        const char* const m_bound_ptr;
    };
}

#endif // __READER_H__
