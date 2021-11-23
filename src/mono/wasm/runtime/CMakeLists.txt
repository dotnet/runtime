cmake_minimum_required(VERSION 3.14.5)

project(mono-wasm-runtime C)

set(CMAKE_EXECUTABLE_SUFFIX ".js")
add_executable(dotnet corebindings.c driver.c pinvoke.c)

target_include_directories(dotnet PUBLIC ${MONO_INCLUDES} ${MONO_OBJ_INCLUDES})
target_compile_options(dotnet PUBLIC @${NATIVE_BIN_DIR}/src/emcc-default.rsp -DCORE_BINDINGS -DGEN_PINVOKE=1)

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
    ${NATIVE_BIN_DIR}/libSystem.Native.a
    ${NATIVE_BIN_DIR}/libSystem.IO.Compression.Native.a)

set_target_properties(dotnet PROPERTIES
    LINK_DEPENDS "${NATIVE_BIN_DIR}/src/emcc-default.rsp;${NATIVE_BIN_DIR}/src/runtime.iffe.js;${SOURCE_DIR}/library-dotnet.js;${SYSTEM_NATIVE_DIR}/pal_random.js"
    LINK_FLAGS "@${NATIVE_BIN_DIR}/src/emcc-default.rsp ${CONFIGURATION_LINK_FLAGS} -DENABLE_NETCORE=1 --extern-pre-js ${NATIVE_BIN_DIR}/src/runtime.iffe.js --js-library ${SOURCE_DIR}/library-dotnet.js --js-library ${SYSTEM_NATIVE_DIR}/pal_random.js"
    RUNTIME_OUTPUT_DIRECTORY "${NATIVE_BIN_DIR}")

if(CMAKE_BUILD_TYPE STREQUAL "Release")
    add_custom_command(TARGET dotnet POST_BUILD COMMAND ${EMSDK_PATH}/upstream/bin/wasm-opt --strip-dwarf ${NATIVE_BIN_DIR}/dotnet.wasm -o ${NATIVE_BIN_DIR}/dotnet.wasm)
endif()
