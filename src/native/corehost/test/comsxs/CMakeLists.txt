# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

add_executable(comsxs
    comsxs.cpp
    App.manifest)

add_sanitizer_runtime_support(comsxs)

target_link_libraries(comsxs PRIVATE hostmisc ole32 oleaut32 shell32 advapi32)

install_with_stripped_symbols(comsxs TARGETS corehost_test)
