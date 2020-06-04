cmake_minimum_required(VERSION 3.12)
project(System.Net.Quic.Native C)

include(ExternalProject)

set(CMAKE_C_STANDARD 99)

set(OPENSSL_ARTIFACT_DIR ${CMAKE_CURRENT_BINARY_DIR}/openssl-artifact)
set(OPENSSL_SOURCE_DIR "${CMAKE_CURRENT_LIST_DIR}/openssl")

if ("${CMAKE_HOST_SYSTEM_NAME}" STREQUAL "Windows")
    if (CMAKE_CL_64)
        set(PLATFORM "win64")
        set(OPENSSL_CONFIG_TARGET "VC-WIN64A")

    else()
        set(PLATFORM "win32")
        set(OPENSSL_CONFIG_TARGET "VC-WIN32")
    endif()

    set(OPENSSL_BUILD_TOOL nmake)
    set(OPENSSL_STATIC_LIBS
        "${OPENSSL_ARTIFACT_DIR}/lib/libssl.lib"
        "${OPENSSL_ARTIFACT_DIR}/lib/libcrypto.lib")

    # static link of openssl requires crypt32.lib, unless "no-stdio" is passed
    set(EXTRA_LIBS "crypt32.lib")

    # disable warnings in order to not fail the build
    set(OPENSSL_CFLAGS /wd4133 /wd4244 /wd4267)
else()
    set(PLATFORM "linux86_64")
    set(OPENSSL_STATIC_LIBS
            "${OPENSSL_ARTIFACT_DIR}/lib/libssl.a"
            "${OPENSSL_ARTIFACT_DIR}/lib/libcrypto.a")
    set(OPENSSL_CONFIG_TARGET linux-x86_64)
    set(OPENSSL_BUILD_TOOL make)
endif()

set(OPENSSL_CONFIG_OPTIONS
    # target must be first
    ${OPENSSL_CONFIG_TARGET}
    # we are interested only in static libs
    no-unit-test no-shared 
    # we still need file IO
    # no-stdio
    # we communicate only through memory, no other IO needed
    no-sock no-dgram 
    # allow only TLS 1.3
    no-ssl no-dtls no-tls1 no-tls1_1 no-tls1_2
    --prefix=${OPENSSL_ARTIFACT_DIR}
    ${OPENSSL_CFLAGS})

ExternalProject_Add(OpenSSL
    SOURCE_DIR "${OPENSSL_SOURCE_DIR}"
    BINARY_DIR "${CMAKE_CURRENT_BINARY_DIR}/openssl-build"
    CONFIGURE_COMMAND perl "${OPENSSL_SOURCE_DIR}/Configure" ${OPENSSL_CONFIG_OPTIONS}
    BUILD_COMMAND ${OPENSSL_BUILD_TOOL}
    INSTALL_COMMAND ${OPENSSL_BUILD_TOOL} install_sw
    TEST_COMMAND "")

include_directories(${OPENSSL_ARTIFACT_DIR}/include)

add_library(System.Net.Quic.Native SHARED library.c library.h)
add_dependencies(System.Net.Quic.Native OpenSSL)

# Allow specification of arguments that should be passed to the linker
if (${__LinkArgs})
    SET_TARGET_PROPERTIES(System.Net.Quic.Native PROPERTIES LINK_FLAGS ${__LinkArgs})
endif()

# Allow specification of libraries that should be linked against
# CMake doesn't like space delimiters as input to target_link_libraries
separate_arguments(linker_libs_sanitized WINDOWS_COMMAND ${__LinkLibraries})
target_link_libraries(System.Net.Quic.Native
    ${EXTRA_LIBS}
    ${linker_libs_sanitized}
    ${OPENSSL_STATIC_LIBS})

install (TARGETS System.Net.Quic.Native DESTINATION .)

if ("${CMAKE_HOST_SYSTEM_NAME}" STREQUAL "Windows")
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE}/System.Net.Quic.Native.pdb DESTINATION .)
endif()
