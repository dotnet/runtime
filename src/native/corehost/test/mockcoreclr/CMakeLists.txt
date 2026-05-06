# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

add_library(mockcoreclr SHARED mockcoreclr.cpp)

if(CLR_CMAKE_TARGET_WIN32)
    target_sources(mockcoreclr PRIVATE
        mockcoreclr.def)
endif()

target_link_libraries(mockcoreclr PRIVATE hostmisc)

target_compile_definitions(mockcoreclr PRIVATE EXPORT_SHARED_API)

install_with_stripped_symbols(mockcoreclr TARGETS corehost_test)
