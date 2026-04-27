# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

add_library(mockaotsdk SHARED mockaotsdk.cpp)

target_link_libraries(mockaotsdk PRIVATE hostmisc)

target_compile_definitions(mockaotsdk PRIVATE EXPORT_SHARED_API)

install_with_stripped_symbols(mockaotsdk TARGETS corehost_test)
