# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(browserhost)
set(DOTNET_PROJECT_NAME "BrowserHost-Static")

# the host library

include(configure.cmake)

set(BROWSERHOST_STATIC_SOURCES
    ./config.h
    ./browserhost.cpp
    ../native.rc
)

add_compile_definitions(FEATURE_APPHOST)
add_compile_definitions(GEN_PINVOKE=1)
add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

add_library(BrowserHost-Static
    STATIC
    ${BROWSERHOST_STATIC_SOURCES}
)
set_target_properties(BrowserHost-Static PROPERTIES OUTPUT_NAME BrowserHost CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(BrowserHost-Static PRIVATE
    hostmisc
)

install(TARGETS BrowserHost-Static DESTINATION sharedFramework COMPONENT runtime)

# Headers included by the CoreCLR native relink generated sources (see BrowserWasmApp.CoreCLR.targets).
install(FILES ${CLR_REPO_ROOT_DIR}/src/coreclr/vm/wasm/callhelpers.hpp DESTINATION sharedFramework/include COMPONENT runtime)
install(FILES ${CLR_SRC_NATIVE_DIR}/minipal/entrypoints.h DESTINATION sharedFramework/include/minipal COMPONENT runtime)
install(FILES ${CLR_SRC_NATIVE_DIR}/minipal/utils.h DESTINATION sharedFramework/include/minipal COMPONENT runtime)
