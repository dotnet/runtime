# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

if (CLR_CMAKE_HOST_APPLE)
    # Prevent exporting symbols from apphost. This is necessary to avoid a problem with
    # local typeinfo for standard C++ EH types like std::bad_alloc, std::out_of_range etc.
    # being exposed for binding for external shared libraries and causing mismatch in
    # catch type matching.
    add_link_options(LINKER:-no_exported_symbols)
endif()

add_subdirectory(standalone)
