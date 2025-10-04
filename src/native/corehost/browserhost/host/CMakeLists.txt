# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(BrowserHost-Static)
set(DOTNET_PROJECT_NAME "BrowserHost-Static")

include(configure.cmake)

set(BROWSERHOST_STATIC_SOURCES
    ./browserhost.cpp
    native.rc
)

set(STATIC_LIB_DESTINATION
    ${CLR_ARTIFACTS_BIN_DIR}/native/net${CMAKE_NET_CORE_APP_CURRENT_VERSION}-browser-${CMAKE_BUILD_TYPE}-wasm/)

add_compile_definitions(FEATURE_APPHOST)
add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

# WASM-TODO set(RUNTIMEINFO_LIB runtimeinfo)

add_library(BrowserHost-Static
    STATIC
    ${BROWSERHOST_STATIC_SOURCES}
)
set_target_properties(BrowserHost-Static PROPERTIES OUTPUT_NAME BrowserHost CLEAN_DIRECT_OUTPUT 1)

# this would build libBrowserHost.js
add_subdirectory(../../../libs/Common/JavaScript common-js)
add_dependencies(BrowserHost-Static System.Native.Browser-Rollup)

target_compile_options(BrowserHost-Static PRIVATE 
        -fwasm-exceptions
        -msimd128
    )
target_link_libraries(BrowserHost-Static PRIVATE
    hostmisc

    ${START_WHOLE_ARCHIVE}
    ${RUNTIMEINFO_LIB}
    ${END_WHOLE_ARCHIVE}
)

install(TARGETS BrowserHost-Static DESTINATION corehost COMPONENT runtime)
install(TARGETS BrowserHost-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
