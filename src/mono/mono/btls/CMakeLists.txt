cmake_minimum_required (VERSION 2.8.10)

project (mono-btls)

if(POLICY CMP0026)
cmake_policy(SET CMP0026 NEW)
endif()
if(POLICY CMP0042)
cmake_policy(SET CMP0042 NEW)
endif()

enable_language(C)
enable_language(CXX)

find_program(CCACHE_PROGRAM ccache)
if(CCACHE_PROGRAM)
    set_property(GLOBAL PROPERTY RULE_LAUNCH_COMPILE "${CCACHE_PROGRAM}")
endif()

# FIXME: cmake's asm detection is broken when using xcrun.
set (CMAKE_ASM_COMPILER "${CMAKE_C_COMPILER}")
set (CMAKE_ASM_COMPILER_ARG1 "${CMAKE_C_COMPILER_ARG1}")
set (CMAKE_ASM_COMPILER_ID "${CMAKE_C_COMPILER_ID}")
enable_language(ASM)

if (NOT "${BTLS_ARCH}" STREQUAL "")
	message (WARNING "SET ARCH: ${BTLS_ARCH}")
	set (CMAKE_SYSTEM_PROCESSOR "${BTLS_ARCH}")
endif ()

set (C_CXX_FLAGS "-Wall -Wsign-compare -Wmissing-field-initializers -fPIC -ggdb -fvisibility=hidden")

set (CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${C_CXX_FLAGS} ${BTLS_CFLAGS}")
set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${C_CXX_FLAGS} ${BTLS_CFLAGS}")
set (CMAKE_ASM_FLAGS "${CMAKE_ASM_FLAGS} ${BTLS_CFLAGS}")
set (CMAKE_MACOSX_RPATH 1)
set (MONO_BTLS 1)

set(BUILD_SHARED_LIBS_SAVED "${BUILD_SHARED_LIBS}")
set(BUILD_SHARED_LIBS OFF)
add_subdirectory (${BTLS_ROOT} boringssl)
set(BUILD_SHARED_LIBS "${BUILD_SHARED_LIBS_SAVED}")

include_directories (
	${SRC_DIR}
	${BTLS_ROOT}/include
)

set (
	MONO_BTLS_SOURCES

	btls-bio.c
	btls-bio.h
	btls-error.c
	btls-error.h
	btls-key.c
	btls-key.h
	btls-pkcs12.c
	btls-pkcs12.h
	btls-ssl-ctx.c
	btls-ssl-ctx.h
	btls-ssl.c
	btls-ssl.h
	btls-time64.c
	btls-util.c
	btls-util.h
	btls-x509-chain.c
	btls-x509-chain.h
	btls-x509-crl.c
	btls-x509-crl.h
	btls-x509-lookup.c
	btls-x509-lookup.h
	btls-x509-lookup-mono.c
	btls-x509-lookup-mono.h
	btls-x509-name.c
	btls-x509-name.h
	btls-x509-revoked.c
	btls-x509-revoked.h
	btls-x509-store-ctx.c
	btls-x509-store-ctx.h
	btls-x509-store.c
	btls-x509-store.h
	btls-x509-verify-param.c
	btls-x509-verify-param.h
	btls-x509.c
	btls-x509.h

	${BORINGSSL_OBJECTS}
)

add_library (mono-btls-shared SHARED ${MONO_BTLS_SOURCES})
