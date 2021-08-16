// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __READER_H__
#define __READER_H__

#include <cstdint>
#include "pal.h"
#include "utils.h"

// support for parsing OSX universal binary headers
#ifdef TARGET_OSX
#include "error_codes.h"
#include <libkern/OSByteOrder.h>
#include <mach-o/fat.h>
#include <mach/machine.h>

#if defined(TARGET_ARM64)
#define TARGET_CPU_TYPE CPU_TYPE_ARM64
#else
#define TARGET_CPU_TYPE CPU_TYPE_X86_64
#endif

template <typename fat_arch_type>
void* offset_in_FAT_universal_binary(const char* addr)
{
    uint32_t nfat_arch = OSSwapBigToHostInt32(((uint32_t*)addr)[1]);

    fat_arch_type* arch_list = (fat_arch_type*)(addr + sizeof(uint32_t) * 2);
    for (int i = 0; i < nfat_arch; i++)
    {
        if (OSSwapBigToHostInt32((uint32_t)arch_list[i].cputype) == TARGET_CPU_TYPE)
        {
            return &arch_list[i].offset;
        }
    }

    trace::error(_X("Couldn't find offset in an universal fat binary."));
    throw StatusCode::BundleExtractionFailure;
}
#endif // TARGET_OSX


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
            m_offset_in_file = 0;

#ifdef TARGET_OSX
            // check for universal binary container and adjust the offset accordingly
            uint32_t magic = OSSwapBigToHostInt32(((uint32_t*)base_ptr)[0]);
            if (magic == FAT_MAGIC)
            {
                m_offset_in_file = OSSwapBigToHostInt32(*(uint32_t*)offset_in_FAT_universal_binary<fat_arch>(base_ptr));
                trace::info(_X("FAT container detected. Offset in file:[%lx]"), m_offset_in_file);
            }
            else if (magic == FAT_MAGIC_64)
            {
                m_offset_in_file = OSSwapBigToHostInt64(*(uint64_t*)offset_in_FAT_universal_binary<fat_arch_64>(base_ptr));
                trace::info(_X("FAT64 container detected. Offset in file:[%lx]"), m_offset_in_file);
            }
#endif

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

        int64_t offset_in_file()
        {
            return m_offset_in_file;
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

        int64_t m_offset_in_file;
    };
}

#endif // __READER_H__
