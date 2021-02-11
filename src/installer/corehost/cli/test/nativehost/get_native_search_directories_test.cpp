// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "get_native_search_directories_test.h"
#include "error_writer_redirector.h"
#include "hostfxr_exports.h"
#include <error_codes.h>

bool get_native_search_directories_test::get_for_command_line(
    const pal::string_t& hostfxr_path,
    int argc,
    const pal::char_t* argv[],
    pal::stringstream_t& test_output)
{
    int rc = StatusCode::Success;
    hostfxr_exports hostfxr{ hostfxr_path };

    error_writer_redirector errors{ hostfxr.set_error_writer };

    int32_t buffer_size = 12;
    if (argc > 0 && pal::strcmp(argv[0], _X("test_NullBufferWithNonZeroSize")) == 0)
    {
        rc = hostfxr.get_native_search_directories(argc, argv, nullptr, 1, &buffer_size);
        test_output << _X("get_native_search_directories (null, 1) returned: ") << std::hex << std::showbase << rc << std::endl;
        test_output << _X("buffer_size: ") << buffer_size << std::endl;
    }
    else if (argc > 0 && pal::strcmp(argv[0], _X("test_NonNullBufferWithNegativeSize")) == 0)
    {
        char_t temp_buffer[10];
        rc = hostfxr.get_native_search_directories(argc, argv, temp_buffer, -1, &buffer_size);
        test_output << _X("get_native_search_directories (temp_buffer, -1) returned: ") << std::hex << std::showbase << rc << std::endl;
        test_output << _X("buffer_size: ") << buffer_size << std::endl;
    }
    else
    {
        rc = hostfxr.get_native_search_directories(argc, argv, nullptr, 0, &buffer_size);
        if (rc != (int)StatusCode::HostApiBufferTooSmall)
        {
            test_output << _X("get_native_search_directories (null,0) returned unexpected error code ") << std::hex << std::showbase << rc << _X(" expected HostApiBufferTooSmall (0x80008098).") << std::endl;
            test_output << _X("buffer_size: ") << buffer_size << std::endl;
            goto Exit;
        }

        std::vector<pal::char_t> buffer;
        buffer.reserve(buffer_size);
        rc = hostfxr.get_native_search_directories(argc, argv, buffer.data(), buffer_size, &buffer_size);
        if (rc != (int)StatusCode::Success)
        {
            test_output << _X("get_native_search_directories returned unexpected error code ") << std::hex << std::showbase << rc << _X(" .") << std::endl;
            goto Exit;
        }

        pal::string_t value(buffer.data());
        test_output << _X("Native search directories: '") << value.c_str() << _X("'");
    }

Exit:
    if (errors.has_errors())
    {
        test_output << _X("hostfxr reported errors:") << std::endl << errors.get_errors().c_str();
    }

    return rc == StatusCode::Success;
}
