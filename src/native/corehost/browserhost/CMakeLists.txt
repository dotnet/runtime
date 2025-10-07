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
add_dependencies(browserhost System.Native.Browser-Rollup)
add_dependencies(browserhost System.Native.Browser-Static)
add_dependencies(browserhost System.Runtime.InteropServices.JavaScript.Native-Static)

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
    # WASM-TODO respect  $(InvariantTimezone)
    # System.Native.TimeZoneData.Invariant
    System.Native.TimeZoneData
)

# WASM-TODO set(RUNTIMEINFO_LIB runtimeinfo)

target_compile_options(browserhost PRIVATE 
        -fwasm-exceptions
        -msimd128
    )

set(JS_SYSTEM_NATIVE_BROWSER
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Native.Browser.js")
set(JS_SYSTEM_BROWSER_UTILS
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Native.Browser/libSystem.Browser.Utils.js")
set(JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE
    "${CLR_ARTIFACTS_OBJ_DIR}/native/browser-${CMAKE_BUILD_TYPE}-wasm/System.Runtime.InteropServices.JavaScript.Native/libSystem.Runtime.InteropServices.JavaScript.Native.js")
set(JS_BROWSER_HOST
    "${CLR_ARTIFACTS_OBJ_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/corehost/libBrowserHost.js")
set(JS_SYSTEM_NATIVE_BROWSER_EXPOST
    "${CMAKE_CURRENT_SOURCE_DIR}/../../libs/System.Native.Browser/libSystem.Native.Browser.extpost.js")

set_target_properties(browserhost PROPERTIES
    LINK_DEPENDS "${JS_SYSTEM_NATIVE_BROWSER};${JS_SYSTEM_BROWSER_UTILS};${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE};${JS_BROWSER_HOST};${JS_SYSTEM_NATIVE_BROWSER_EXPOST};"
    LINK_FLAGS "--js-library ${JS_SYSTEM_NATIVE_BROWSER} --js-library ${JS_SYSTEM_BROWSER_UTILS} --js-library ${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE} --js-library ${JS_BROWSER_HOST} --extern-post-js ${JS_SYSTEM_NATIVE_BROWSER_EXPOST}"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}")

if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    target_link_options(browserhost PRIVATE
#        -sVERBOSE
#        -sASSERTIONS=1
        --emit-symbol-map
        -sLLD_REPORT_UNDEFINED
        -sERROR_ON_UNDEFINED_SYMBOLS=1
    )
endif ()

# WASM-TODO    -sWASM_BIGINT=1
# WASM-TODO    -emit-llvm
# WASM-TODO    --source-map-base http://microsoft.com
target_link_options(browserhost PRIVATE
    -sINITIAL_MEMORY=134217728
    -sMAXIMUM_MEMORY=2147483648
    -sALLOW_MEMORY_GROWTH=1
    -sALLOW_TABLE_GROWTH=1
    -sSTACK_SIZE=5MB
    -sMODULARIZE=1
    -sEXPORT_ES6=1
    -sEXIT_RUNTIME=0
    -sEXPORTED_RUNTIME_METHODS=UTF8ToString,cwrap,ccall,HEAPU8,HEAPU32,HEAPU64,BROWSER_HOST
    -sEXPORTED_FUNCTIONS=_posix_memalign,_free,stackAlloc,stackRestore,stackSave,_BrowserHost_InitializeCoreCLR,_BrowserHost_ExecuteAssembly,___cpp_exception
    -sEXPORT_NAME=createDotnetRuntime
    -lnodefs.js
    -Wno-unused-command-line-argument
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

add_subdirectory(sample)
