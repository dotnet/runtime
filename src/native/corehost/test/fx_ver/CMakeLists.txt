# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
include_directories(../../fxr)

add_executable(test_fx_ver test_fx_ver.cpp)

add_sanitizer_runtime_support(test_fx_ver)

target_link_libraries(test_fx_ver PRIVATE libhostcommon hostmisc)

install_with_stripped_symbols(test_fx_ver TARGETS corehost_test)
