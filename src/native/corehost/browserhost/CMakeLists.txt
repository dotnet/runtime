# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(browserhost)
set(DOTNET_PROJECT_NAME "browserhost")

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
    System.Native.Browser-Static
    System.Runtime.InteropServices.JavaScript.Native-Static
    System.Native-Static
    System.Globalization.Native-Static
    System.IO.Compression.Native-Static
    nativeresourcestring
    gcinfo
    # WASMTODO respect  $(InvariantTimezone)
    # System.Native.TimeZoneData.Invariant
    System.Native.TimeZoneData
)

# WASMTODO set(RUNTIMEINFO_LIB runtimeinfo)

target_compile_options(browserhost PRIVATE -fwasm-exceptions)

set_target_properties(browserhost PROPERTIES
    LINK_DEPENDS "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Native.Browser.js;${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js;${CMAKE_CURRENT_SOURCE_DIR}/libBrowserHost.js;${CMAKE_CURRENT_SOURCE_DIR}/../../libs/System.Native.Browser/libSystem.Native.Browser.extpost.js;"
    LINK_FLAGS "--js-library ${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Native.Browser.js --js-library ${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js --js-library ${CMAKE_CURRENT_SOURCE_DIR}/libBrowserHost.js --extern-post-js ${CMAKE_CURRENT_SOURCE_DIR}/../../libs/System.Native.Browser/libSystem.Native.Browser.extpost.js "
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}")

# WASMTODO -Oz -sVERBOSE
target_link_options(browserhost PRIVATE
    -fwasm-exceptions
    -sEXIT_RUNTIME=0
    -sINITIAL_MEMORY=134217728
    -sSTACK_SIZE=5MB
    -sMODULARIZE=1
    -sEXPORT_ES6=1
    -sEXPORTED_RUNTIME_METHODS=UTF8ToString,cwrap,ccall,HEAPU8,HEAPU32,HEAPU64,BROWSER_HOST
    -sEXPORTED_FUNCTIONS=_posix_memalign,_free,stackAlloc,stackRestore,stackSave,_browserHostInitializeCoreCLR,_browserHostExecuteAssembly
    -sEXPORT_NAME=createDotnetRuntime
    -lnodefs.js
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

# WASM-TODO: this is temporary locations
set(SAMPLE_ASSETS
    sample/index.html
    sample/main.mjs
    sample/dotnet.boot.js
    # ${CLR_ARTIFACTS_OBJ_DIR}/sfx-finish/${CMAKE_BUILD_TYPE}/net10.0-browser/illink-wasm/System.Console.dll
    # ${CLR_ARTIFACTS_OBJ_DIR}/sfx-finish/${CMAKE_BUILD_TYPE}/net10.0-browser/illink-wasm/System.Private.CoreLib.dll
    # ${CLR_ARTIFACTS_OBJ_DIR}/sfx-finish/${CMAKE_BUILD_TYPE}/net10.0-browser/illink-wasm/System.Runtime.dll
    # ${CLR_ARTIFACTS_OBJ_DIR}/sfx-finish/${CMAKE_BUILD_TYPE}/net10.0-browser/illink-wasm/System.Runtime.InteropServices.dll
    # ${CLR_ARTIFACTS_OBJ_DIR}/sfx-finish/${CMAKE_BUILD_TYPE}/net10.0-browser/illink-wasm/System.Threading.dll
    # ${CLR_ARTIFACTS_BIN_DIR}/artifacts/bin/HelloWorld/${CMAKE_BUILD_TYPE}/net10.0/win-x64/HelloWorld.dll
    ${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.js
    # WASM-TODO ${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.js.map
    ${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/dotnet.d.ts
    ${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js
    # WASM-TODO ${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/dotnet.runtime.js.map
)
install(FILES ${SAMPLE_ASSETS} DESTINATION corehost COMPONENT runtime)
