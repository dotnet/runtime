cmake_minimum_required(VERSION 3.14.5)

project(mono-wasi-runtime C)

option(DISABLE_THREADS "defined if the build does NOT support multithreading" ON)
option(DISABLE_WASM_USER_THREADS "defined if the build does not allow user threads to be created in a multithreaded build" OFF)

add_executable(dotnet driver.c stubs.c synthetic-pthread.c)

target_include_directories(dotnet PUBLIC ${MONO_INCLUDES} ${MONO_OBJ_INCLUDES} ${CMAKE_CURRENT_BINARY_DIR}/include/wasm)
target_compile_options(dotnet PUBLIC @${NATIVE_BIN_DIR}/src/wasi-default.rsp @${NATIVE_BIN_DIR}/src/wasi-compile.rsp -DCORE_BINDINGS -DGEN_PINVOKE=1)

set_target_properties(dotnet PROPERTIES COMPILE_FLAGS ${CONFIGURATION_WASICC_FLAGS})

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
#TODOWASI    ${NATIVE_BIN_DIR}/libSystem.Native.a
#TODOWASI    ${NATIVE_BIN_DIR}/libSystem.IO.Compression.Native.a
)

set_target_properties(dotnet PROPERTIES
    LINK_DEPENDS "${NATIVE_BIN_DIR}/src/wasi-default.rsp;"
    LINK_FLAGS "@${NATIVE_BIN_DIR}/src/wasi-default.rsp @${NATIVE_BIN_DIR}/src/wasi-link.rsp ${CONFIGURATION_LINK_FLAGS} "
    RUNTIME_OUTPUT_DIRECTORY "${NATIVE_BIN_DIR}")

set(ignoreMeWasmOptFlags "${CONFIGURATION_WASM_OPT_FLAGS}")  

#TODOWASI wasm-opt

configure_file(../include/wasm-config.h.in include/wasm/wasm-config.h)
