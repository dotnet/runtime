set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

if(CLR_CMAKE_TARGET_ANDROID OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_ARCH_WASM)
    return()
endif()

include_directories(${CLR_SRC_NATIVE_DIR})

set(NATIVE_TEST_COMMON_DIR ../tracing/eventpipe/native/common)
set(CORECLR_MINIPAL_DIR ${CLR_REPO_ROOT_DIR}/src/coreclr/minipal)

if(CLR_CMAKE_HOST_WIN32)
    set(DN_U16_SOURCE ${CORECLR_MINIPAL_DIR}/Windows/dn-u16.cpp)
else()
    set(DN_U16_SOURCE ${CORECLR_MINIPAL_DIR}/Unix/dn-u16.cpp)
endif()

add_executable(NativeMinipalTestsDriver
    ${NATIVE_TEST_COMMON_DIR}/driver.c
    ${NATIVE_TEST_COMMON_DIR}/test.c
    native-minipal-tests-driver.c
    utf8-tests.c
    u16-tests.cpp
    ${DN_U16_SOURCE})

target_include_directories(NativeMinipalTestsDriver PRIVATE ${NATIVE_TEST_COMMON_DIR} ${CORECLR_MINIPAL_DIR})
target_link_libraries(NativeMinipalTestsDriver PRIVATE minipal)
install(TARGETS NativeMinipalTestsDriver DESTINATION bin)
