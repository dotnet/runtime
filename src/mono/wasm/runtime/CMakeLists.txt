cmake_minimum_required(VERSION 3.14.5)

project(mono-wasm-runtime C)

option(DISABLE_THREADS "defined if the build does NOT support multithreading" ON)
option(DISABLE_WASM_USER_THREADS "defined if the build does not allow user threads to be created in a multithreaded build" OFF)

set(CMAKE_EXECUTABLE_SUFFIX ".js")
add_executable(dotnet corebindings.c driver.c pinvoke.c)

target_include_directories(dotnet PUBLIC ${MONO_INCLUDES} ${MONO_OBJ_INCLUDES} ${CMAKE_CURRENT_BINARY_DIR}/include/wasm)
target_compile_options(dotnet PUBLIC @${NATIVE_BIN_DIR}/src/emcc-default.rsp @${NATIVE_BIN_DIR}/src/emcc-compile.rsp -DCORE_BINDINGS -DGEN_PINVOKE=1)

set_target_properties(dotnet PROPERTIES COMPILE_FLAGS ${CONFIGURATION_EMCC_FLAGS})

target_link_libraries(dotnet
    ${ICU_LIB_DIR}/libicuuc.a
    ${ICU_LIB_DIR}/libicui18n.a
    ${MONO_ARTIFACTS_DIR}/libmono-component-hot_reload-static.a
    ${MONO_ARTIFACTS_DIR}/libmono-component-debugger-static.a
    ${MONO_ARTIFACTS_DIR}/libmono-component-diagnostics_tracing-stub-static.a
    ${MONO_ARTIFACTS_DIR}/libmono-ee-interp.a
    ${MONO_ARTIFACTS_DIR}/libmonosgen-2.0.a
    ${MONO_ARTIFACTS_DIR}/libmono-ilgen.a
    ${MONO_ARTIFACTS_DIR}/libmono-icall-table.a
    ${MONO_ARTIFACTS_DIR}/libmono-wasm-eh-js.a
    ${MONO_ARTIFACTS_DIR}/libmono-profiler-aot.a
    ${NATIVE_BIN_DIR}/libSystem.Native.a
    ${NATIVE_BIN_DIR}/libSystem.IO.Compression.Native.a
    ${NATIVE_BIN_DIR}/libSystem.Security.Cryptography.Native.Browser.a)

set_target_properties(dotnet PROPERTIES
    LINK_DEPENDS "${NATIVE_BIN_DIR}/src/emcc-default.rsp;${NATIVE_BIN_DIR}/src/es6/dotnet.es6.pre.js;${NATIVE_BIN_DIR}/src/es6/runtime.es6.iffe.js;${NATIVE_BIN_DIR}/src/es6/dotnet.es6.lib.js;${NATIVE_BIN_DIR}/src/pal_random.lib.js;${NATIVE_BIN_DIR}/src/es6/dotnet.es6.post.js;"
    LINK_FLAGS "@${NATIVE_BIN_DIR}/src/emcc-default.rsp @${NATIVE_BIN_DIR}/src/emcc-link.rsp ${CONFIGURATION_LINK_FLAGS} --extern-pre-js ${NATIVE_BIN_DIR}/src/es6/runtime.es6.iffe.js --pre-js ${NATIVE_BIN_DIR}/src/es6/dotnet.es6.pre.js --js-library ${NATIVE_BIN_DIR}/src/es6/dotnet.es6.lib.js --js-library ${NATIVE_BIN_DIR}/src/pal_random.lib.js --post-js ${NATIVE_BIN_DIR}/src/es6/dotnet.es6.post.js "
    RUNTIME_OUTPUT_DIRECTORY "${NATIVE_BIN_DIR}")

set(ignoreMeWasmOptFlags "${CONFIGURATION_WASM_OPT_FLAGS}")  

if(CMAKE_BUILD_TYPE STREQUAL "Release")
    add_custom_command(TARGET dotnet
                        POST_BUILD COMMAND ${EMSDK_PATH}/upstream/bin/wasm-opt --enable-exception-handling ${CONFIGURATION_WASM_OPT_FLAGS} --strip-dwarf ${NATIVE_BIN_DIR}/dotnet.wasm -o ${NATIVE_BIN_DIR}/dotnet.wasm
                        COMMENT "Stripping debug symbols from dotnet.wasm using wasm-opt")
endif()

configure_file(wasm-config.h.in include/wasm/wasm-config.h)
