# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(BrowserHost-Static)
set(DOTNET_PROJECT_NAME "BrowserHost-Static")

include(configure.cmake)

set(BROWSERHOST_STATIC_SOURCES
    ./config.h
    ./browserhost.cpp
    ../../native.rc
)

add_compile_definitions(FEATURE_APPHOST)
add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

add_library(BrowserHost-Static
    STATIC
    ${BROWSERHOST_STATIC_SOURCES}
)
set_target_properties(BrowserHost-Static PROPERTIES OUTPUT_NAME BrowserHost CLEAN_DIRECT_OUTPUT 1)

target_compile_options(BrowserHost-Static PRIVATE 
        -fwasm-exceptions
        -msimd128
    )
target_link_libraries(BrowserHost-Static PRIVATE
    hostmisc

    ${START_WHOLE_ARCHIVE}
    # WASM-TODO: runtimeinfo
    ${END_WHOLE_ARCHIVE}
)

install(TARGETS BrowserHost-Static DESTINATION corehost COMPONENT runtime)
install(TARGETS BrowserHost-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
install(TARGETS BrowserHost-Static DESTINATION sharedFramework COMPONENT runtime)
