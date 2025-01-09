# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

add_library(ijw SHARED ijw.cpp)
target_link_libraries(ijw PRIVATE ijwhost)

remove_ijw_incompatible_target_options(ijw)
add_ijw_msbuild_project_properties(ijw ijwhost)

install_with_stripped_symbols(ijw TARGETS corehost_test)
