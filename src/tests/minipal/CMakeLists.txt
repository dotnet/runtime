set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

if(CLR_CMAKE_TARGET_ANDROID OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_ARCH_WASM)
    return()
endif()

include_directories(${CLR_SRC_NATIVE_DIR})

set(NATIVE_TEST_COMMON_DIR ../tracing/eventpipe/native/common)

add_executable(NativeMinipalTestsDriver
    ${NATIVE_TEST_COMMON_DIR}/driver.c
    ${NATIVE_TEST_COMMON_DIR}/test.c
    native-minipal-tests-driver.c
    utf8-tests.c)

target_include_directories(NativeMinipalTestsDriver PRIVATE ${NATIVE_TEST_COMMON_DIR})
target_link_libraries(NativeMinipalTestsDriver PRIVATE minipal)
install(TARGETS NativeMinipalTestsDriver DESTINATION bin)
