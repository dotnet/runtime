# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(browserhost)
set(DOTNET_PROJECT_NAME "browserhost")

set(BROWSERHOST_SOURCES
    empty.c
)
set(HEADERS
)

add_compile_definitions(FEATURE_APPHOST)
add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

add_executable(browserhost ${BROWSERHOST_SOURCES})
set_target_properties(browserhost PROPERTIES OUTPUT_NAME dotnet.native)
set(CMAKE_EXECUTABLE_SUFFIX ".js")

add_subdirectory(host)
add_dependencies(browserhost BrowserHost-Static)

set(STATIC_LIB_DESTINATION
    ${CLR_ARTIFACTS_BIN_DIR}/native/net${CMAKE_NET_CORE_APP_CURRENT_VERSION}-browser-${CMAKE_BUILD_TYPE}-wasm/)
set(STATIC_CLR_DESTINATION
    ${CLR_ARTIFACTS_BIN_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_TYPE}/)

# these dependencies assume that you built `libs.native+clr.runtime` subsets first
LIST(APPEND NATIVE_LIBS
    ${STATIC_CLR_DESTINATION}/libcoreclr_static.a
    ${STATIC_CLR_DESTINATION}/libclrinterpreter.a
    ${STATIC_CLR_DESTINATION}/lib/libnativeresourcestring.a
    ${STATIC_CLR_DESTINATION}/libcoreclrminipal.a
    ${STATIC_CLR_DESTINATION}/lib/libcoreclrpal.a
    ${STATIC_CLR_DESTINATION}/gcinfo/libgcinfo_unix_wasm.a
    ${STATIC_LIB_DESTINATION}/lib/libminipal.a
    ${STATIC_LIB_DESTINATION}/libSystem.Native.Browser.a
    ${STATIC_LIB_DESTINATION}/libSystem.Runtime.InteropServices.JavaScript.Native.a
    ${STATIC_LIB_DESTINATION}/libSystem.Native.a
    ${STATIC_LIB_DESTINATION}/libSystem.Globalization.Native.a
    ${CMAKE_ICU_DIR}/lib/libicuuc.a
    ${CMAKE_ICU_DIR}/lib/libicui18n.a
    ${CMAKE_ICU_DIR}/lib/libicudata.a
    ${STATIC_LIB_DESTINATION}/libSystem.IO.Compression.Native.a
    ${STATIC_LIB_DESTINATION}/libz.a
    # WASM-TODO respect  $(InvariantTimezone)
    # libSystem.Native.TimeZoneData.Invariant.a
    ${STATIC_LIB_DESTINATION}/libSystem.Native.TimeZoneData.a
)

set(NATIVE_JAVASCRIPT_BIN
    ${CLR_ARTIFACTS_BIN_DIR}/native/net${CMAKE_NET_CORE_APP_CURRENT_VERSION}-browser-${CMAKE_BUILD_TYPE}-wasm/)
set(JS_SYSTEM_NATIVE_BROWSER
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Native.Browser.js")
set(JS_SYSTEM_BROWSER_UTILS
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Browser.Utils.js")
set(JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE
    "${NATIVE_JAVASCRIPT_BIN}/libSystem.Runtime.InteropServices.JavaScript.Native.js")
set(JS_BROWSER_HOST
    "${NATIVE_JAVASCRIPT_BIN}/libBrowserHost.js")
set(JS_SYSTEM_NATIVE_BROWSER_EXPOST
    "${CMAKE_CURRENT_SOURCE_DIR}/../../libs/System.Native.Browser/libSystem.Native.Browser.extpost.js")

set_target_properties(browserhost PROPERTIES
    LINK_DEPENDS "${JS_SYSTEM_NATIVE_BROWSER};${JS_SYSTEM_BROWSER_UTILS};${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE};${JS_BROWSER_HOST};${JS_SYSTEM_NATIVE_BROWSER_EXPOST};"
    LINK_FLAGS "--js-library ${JS_SYSTEM_NATIVE_BROWSER} --js-library ${JS_SYSTEM_BROWSER_UTILS} --js-library ${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE} --js-library ${JS_BROWSER_HOST} --extern-post-js ${JS_SYSTEM_NATIVE_BROWSER_EXPOST}"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}")

target_compile_options(browserhost PRIVATE
        -fwasm-exceptions
        -msimd128
    )

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
    -fwasm-exceptions
    -msimd128
    -lnodefs.js
    -Wno-unused-command-line-argument
    -Wl,-error-limit=0)

target_link_libraries(browserhost PUBLIC
    BrowserHost-Static
)
target_link_libraries(browserhost PRIVATE
    ${NATIVE_LIBS}
)

install(TARGETS browserhost DESTINATION corehost COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.wasm DESTINATION corehost COMPONENT runtime)

# this is for liveBuilds.targets
install(TARGETS browserhost DESTINATION ${STATIC_LIB_DESTINATION})
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.wasm DESTINATION ${STATIC_LIB_DESTINATION})
install(FILES ${CLR_SRC_NATIVE_DIR}/package.json DESTINATION ${STATIC_LIB_DESTINATION})

add_subdirectory(sample)
