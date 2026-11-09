set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

if(CLR_CMAKE_TARGET_ANDROID OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_ARCH_WASM)
    return()
endif()

if(NOT TARGET dn-containers)
    add_subdirectory(${CLR_SRC_NATIVE_DIR}/containers containers-library)
endif()

if(NOT TARGET dn-eventpipe)
    set(EP_GENERATED_HEADER_PATH "${CMAKE_CURRENT_BINARY_DIR}")
    include(${CLR_SRC_NATIVE_DIR}/eventpipe/configure.cmake)
    add_subdirectory(${CLR_SRC_NATIVE_DIR}/eventpipe eventpipe-library)
endif()

find_package(Threads REQUIRED)

add_executable(NativeEventPipeTestsDriver
    ../common/driver.c
    ../common/test.c
    native-eventpipe-tests-driver.c
    ep-rt-test.c
    ep-buffer-manager-tests.c
    ep-buffer-tests.c
    ep-fastserializer-tests.c
    ep-file-tests.c
    ep-provider-callback-dataqueue-tests.c
    ep-rt-tests.c
    ep-session-tests.c
    ep-tests.c
    ep-thread-tests.c)

option(TEST_PERF "Run native EventPipe performance tests in Release builds" ON)
target_compile_definitions(NativeEventPipeTestsDriver PRIVATE
    ENABLE_PERFTRACING
    EP_CUSTOM_RT_CONFIG
    EP_RT_H=\"ep-rt-test.h\"
    EP_RT_TYPES_H=\"ep-rt-types-test.h\"
    EP_RT_CONFIG_H=\"ep-rt-config-test.h\"
    TEST_PERF=$<AND:$<CONFIG:Release>,$<BOOL:${TEST_PERF}>>)

target_include_directories(NativeEventPipeTestsDriver PRIVATE ${CMAKE_CURRENT_SOURCE_DIR} ../common)
target_link_libraries(NativeEventPipeTestsDriver PRIVATE dn-eventpipe dn-containers minipal Threads::Threads)
install(TARGETS NativeEventPipeTestsDriver DESTINATION bin)
