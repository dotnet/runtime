# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(browserhost)
set(DOTNET_PROJECT_NAME "browserhost")

include_directories(..)
include_directories(../hostmisc)
#WASMTODO include_directories(../hostpolicy)
#WASMTODO include_directories(../json)
include_directories(${CLR_SRC_NATIVE_DIR}/libs/Browser.Native)
include_directories(${CLR_SRC_NATIVE_DIR}/libs/System.IO.Compression.Native)
include_directories(${CLR_SRC_NATIVE_DIR}/libs/Common)
include_directories(${CLR_ARTIFACTS_OBJ_DIR}) # Generated version files

add_subdirectory(../hostmisc hostmisc)

configure_file(${CLR_SRC_NATIVE_DIR}/corehost/configure.h.in ${GENERATED_INCLUDE_DIR}/corehost/configure.h)
target_include_directories(hostmisc_interface INTERFACE ${GENERATED_INCLUDE_DIR}/corehost)

set(SOURCES
    ./browserhost.cpp
    native.rc
)
set(HEADERS
)

add_compile_definitions(FEATURE_APPHOST)
add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

add_executable(browserhost ${SOURCES})
set_target_properties(browserhost PROPERTIES OUTPUT_NAME dotnet.native)
set(CMAKE_EXECUTABLE_SUFFIX ".js")

install(TARGETS browserhost DESTINATION corehost COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.wasm DESTINATION corehost COMPONENT runtime)

include(configure.cmake)

LIST(APPEND NATIVE_LIBS
    coreclr_static
    Browser.Native-Static
    System.Native-Static
    System.Globalization.Native-Static
    System.IO.Compression.Native-Static
    nativeresourcestring
    gcinfo
    # WASMTODO do not link icudata and download shard at runtime
    # WASMTODO respect  $(InvariantTimezone)
    # System.Native.TimeZoneData.Invariant
    System.Native.TimeZoneData
)

# WASMTODO set(RUNTIMEINFO_LIB runtimeinfo)

target_compile_options(browserhost PRIVATE -fwasm-exceptions)

# WASMTODO linker options for NodeJs for now
target_link_options(browserhost PRIVATE
    -fwasm-exceptions
    -sEXPORTED_RUNTIME_METHODS=FS
    -sEXIT_RUNTIME=1
    -sINITIAL_MEMORY=134217728
    -sSTACK_SIZE=5MB
    -lnoderawfs.js
    -lnodefs.js
    --pre-js ${CMAKE_CURRENT_SOURCE_DIR}/browserhost.js
    --js-library ${CLR_SRC_NATIVE_DIR}/libs/Browser.Native/browser.native.js
    -Wl,-error-limit=0)

target_link_libraries(
    browserhost
    PRIVATE
    hostmisc
    clrinterpreter
    ${NATIVE_LIBS}

    ${START_WHOLE_ARCHIVE}
    ${RUNTIMEINFO_LIB}
    ${END_WHOLE_ARCHIVE}
)
