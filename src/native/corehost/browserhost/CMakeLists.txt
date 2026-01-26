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

# the executable
set(DOTNET_PROJECT_NAME "browserhost")

set(BROWSERHOST_SOURCES
    empty.c
)
set(HEADERS
)

add_compile_definitions(FEATURE_APPHOST)
add_definitions(-DGEN_PINVOKE=1)
add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

add_executable(browserhost ${BROWSERHOST_SOURCES})
set_target_properties(browserhost PROPERTIES OUTPUT_NAME dotnet.native)
set(CMAKE_EXECUTABLE_SUFFIX ".js")



set(SHARED_LIB_DESTINATION
    ${CLR_ARTIFACTS_BIN_DIR}/native/net${CMAKE_NET_CORE_APP_CURRENT_VERSION}-browser-${CMAKE_BUILD_LIBRARIES_CONFIGURATION}-wasm/sharedFramework)
set(SHARED_CLR_DESTINATION
    ${CLR_ARTIFACTS_BIN_DIR}/coreclr/browser.wasm.${CMAKE_BUILD_RUNTIME_CONFIGURATION}/sharedFramework)

# these dependencies assume that you built `libs.native+clr.runtime` subsets first
LIST(APPEND NATIVE_LIBS
    ${SHARED_CLR_DESTINATION}/libcoreclr_static.a
    ${SHARED_CLR_DESTINATION}/libnativeresourcestring.a
    ${SHARED_CLR_DESTINATION}/libgcinfo_unix_wasm.a
    ${SHARED_CLR_DESTINATION}/libcoreclrminipal.a
    ${SHARED_CLR_DESTINATION}/libcoreclrpal.a
    ${SHARED_CLR_DESTINATION}/libminipal.a
    ${SHARED_LIB_DESTINATION}/libSystem.Native.Browser.a
    ${SHARED_LIB_DESTINATION}/libSystem.Runtime.InteropServices.JavaScript.Native.a
    ${SHARED_LIB_DESTINATION}/libSystem.Native.a
    ${SHARED_LIB_DESTINATION}/libSystem.Globalization.Native.a
    ${CMAKE_ICU_DIR}/lib/libicuuc.a
    ${CMAKE_ICU_DIR}/lib/libicui18n.a
    ${CMAKE_ICU_DIR}/lib/libicudata.a
    ${SHARED_LIB_DESTINATION}/libSystem.IO.Compression.Native.a
    ${SHARED_LIB_DESTINATION}/libz.a
    # WASM-TODO respect  $(InvariantTimezone)
    # libSystem.Native.TimeZoneData.Invariant.a
    ${SHARED_LIB_DESTINATION}/libSystem.Native.TimeZoneData.a
)

set(JS_SYSTEM_NATIVE_BROWSER
    "${SHARED_LIB_DESTINATION}/libSystem.Native.Browser.js")
set(JS_SYSTEM_BROWSER_UTILS
    "${SHARED_LIB_DESTINATION}/libSystem.Native.Browser.Utils.js")
set(JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE
    "${SHARED_LIB_DESTINATION}/libSystem.Runtime.InteropServices.JavaScript.Native.js")
set(JS_BROWSER_HOST
    "${SHARED_LIB_DESTINATION}/libBrowserHost.js")
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

# add -sVERBOSE=1 to debug linking issues
# WASM-TODO    -emit-llvm
# WASM-TODO    --source-map-base http://microsoft.com
# WASM-TODO    -sSEPARATE_DWARF=1, -gseparate-dwarf
target_link_options(browserhost PRIVATE
    -sINITIAL_MEMORY=134217728
    -sMAXIMUM_MEMORY=2147483648
    -sALLOW_MEMORY_GROWTH=1
    -sSTACK_SIZE=5MB
    -sWASM_BIGINT=1
    -sMODULARIZE=1
    -sEXPORT_ES6=1
    -sEXIT_RUNTIME=1
    -sALLOW_TABLE_GROWTH=1
    -sEXPORTED_RUNTIME_METHODS=BROWSER_HOST,${CMAKE_EMCC_EXPORTED_RUNTIME_METHODS}
    -sEXPORTED_FUNCTIONS=${CMAKE_EMCC_EXPORTED_FUNCTIONS}
    -sEXPORT_NAME=createDotnetRuntime
    -sENVIRONMENT=web,webview,worker,node,shell
    -lnodefs.js
    -Wl,-error-limit=0)

target_link_libraries(browserhost PRIVATE
    BrowserHost-Static
    ${NATIVE_LIBS}
	${START_WHOLE_ARCHIVE}
    ${RUNTIMEINFO_LIB}
    ${END_WHOLE_ARCHIVE}
)

install(TARGETS browserhost DESTINATION corehost COMPONENT runtime)
install(TARGETS browserhost DESTINATION sharedFramework COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.wasm DESTINATION corehost COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.wasm DESTINATION sharedFramework COMPONENT runtime)

set_source_files_properties(${BROWSERHOST_SOURCES} PROPERTIES OBJECT_DEPENDS
    "${JS_SYSTEM_NATIVE_BROWSER};${JS_SYSTEM_BROWSER_UTILS};${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE};${JS_BROWSER_HOST};${JS_SYSTEM_NATIVE_BROWSER_EXPOST}")
