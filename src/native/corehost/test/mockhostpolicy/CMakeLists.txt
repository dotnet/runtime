# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

add_library(mockhostpolicy SHARED mockhostpolicy.cpp)

target_link_libraries(mockhostpolicy PRIVATE hostmisc)

target_compile_definitions(mockhostpolicy PRIVATE EXPORT_SHARED_API)

install_with_stripped_symbols(mockhostpolicy TARGETS corehost_test)