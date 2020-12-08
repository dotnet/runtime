// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "reader.h"
#include "error_codes.h"
#include "trace.h"

using namespace bundle;

const char* reader_t::add_without_overflow(const char* ptr, int64_t len)
{
    const char* new_ptr = ptr + len;

    // The following check will fail in case len < 0 (which is also an error while reading) 
    // even if the actual arthmetic didn't overflow.
    if (new_ptr < ptr)
    {
        trace::error(_X("Failure processing application bundle; possible file corruption."));
        trace::error(_X("Arithmetic overflow computing bundle-bounds."));
        throw StatusCode::BundleExtractionFailure;
    }

    return new_ptr;
}

void reader_t::set_offset(int64_t offset)
{
    if (offset < 0 || offset >= m_bound)
    {
        trace::error(_X("Failure processing application bundle; possible file corruption."));
        trace::error(_X("Arithmetic overflow while reading bundle."));
        throw StatusCode::BundleExtractionFailure;
    }

    m_ptr = m_base_ptr + offset;
}

void reader_t::bounds_check(int64_t len)
{
    const char* post_read_ptr = add_without_overflow(m_ptr, len);
    
    // It is legal for post_read_ptr == m_bound_ptr after reading the last byte.
    if (m_ptr < m_base_ptr || post_read_ptr > m_bound_ptr)
    {
        trace::error(_X("Failure processing application bundle; possible file corruption."));
        trace::error(_X("Bounds check failed while reading the bundle."));
        throw StatusCode::BundleExtractionFailure;
    }
}

// Handle the relatively uncommon scenario where the bundle ID or 
// the relative-path of a file within the bundle is longer than 127 bytes
size_t reader_t::read_path_length()
{
    size_t length = 0;

    int8_t first_byte = read();

    // If the high bit is set, it means there are more bytes to read.
    if ((first_byte & 0x80) == 0)
    {
         length = first_byte;
    }
    else
    {
        int8_t second_byte = read();

        if (second_byte & 0x80)
        {
            // There can be no more than two bytes in path_length
            trace::error(_X("Failure processing application bundle; possible file corruption."));
            trace::error(_X("Path length encoding read beyond two bytes."));

            throw StatusCode::BundleExtractionFailure;
        }

        length = (second_byte << 7) | (first_byte & 0x7f);
    }

    if (length <= 0 || length > PATH_MAX)
    {
        trace::error(_X("Failure processing application bundle; possible file corruption."));
        trace::error(_X("Path length is zero or too long."));
        throw StatusCode::BundleExtractionFailure;
    }

    return length;
}

size_t reader_t::read_path_string(pal::string_t &str)
{
    const char* start_ptr = m_ptr;
    size_t size = read_path_length();
    std::unique_ptr<uint8_t[]> buffer{ new uint8_t[size + 1] };
    read(buffer.get(), size);
    buffer[size] = 0; // null-terminator
    pal::clr_palstring(reinterpret_cast<const char*>(buffer.get()), &str);

    return m_ptr - start_ptr; // This subtraction can't overflow because addition above is bounds_checked
}
