cmake_minimum_required(VERSION 3.14.5)

project(mono-wasm-runtime C)

set(CMAKE_EXECUTABLE_SUFFIX ".js")
add_executable(dotnet corebindings.c driver.c pinvoke.c)

target_include_directories(dotnet PUBLIC ${MONO_INCLUDES} ${MONO_OBJ_INCLUDES})
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
    ${NATIVE_BIN_DIR}/libSystem.IO.Compression.Native.a)

set_target_properties(dotnet PROPERTIES
    LINK_DEPENDS "${NATIVE_BIN_DIR}/src/emcc-default.rsp;${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.pre.js;${NATIVE_BIN_DIR}/src/cjs/runtime.cjs.iffe.js;${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.lib.js;${NATIVE_BIN_DIR}/src/pal_random.lib.js;${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.post.js;${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.extpost.js;"
    LINK_FLAGS "@${NATIVE_BIN_DIR}/src/emcc-default.rsp @${NATIVE_BIN_DIR}/src/emcc-link.rsp ${CONFIGURATION_LINK_FLAGS} --extern-pre-js ${NATIVE_BIN_DIR}/src/cjs/runtime.cjs.iffe.js --pre-js ${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.pre.js --js-library ${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.lib.js --js-library ${NATIVE_BIN_DIR}/src/pal_random.lib.js --post-js ${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.post.js --extern-post-js ${NATIVE_BIN_DIR}/src/cjs/dotnet.cjs.extpost.js "
    RUNTIME_OUTPUT_DIRECTORY "${NATIVE_BIN_DIR}")

if(CMAKE_BUILD_TYPE STREQUAL "Release")
    add_custom_command(TARGET dotnet POST_BUILD COMMAND ${EMSDK_PATH}/upstream/bin/wasm-opt --enable-exception-handling --strip-dwarf ${NATIVE_BIN_DIR}/dotnet.wasm -o ${NATIVE_BIN_DIR}/dotnet.wasm)
endif()
