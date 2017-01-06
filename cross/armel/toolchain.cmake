set(CROSS_ROOTFS $ENV{ROOTFS_DIR})

set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_VERSION 1)
set(CMAKE_SYSTEM_PROCESSOR armv7l)

## Specify the toolchain
set(TOOLCHAIN "arm-linux-gnueabi")
set(TOOLCHAIN_PREFIX ${TOOLCHAIN}-)

add_compile_options(-target armv7-linux-gnueabi)
add_compile_options(-mthumb)
add_compile_options(-mfpu=vfpv3)
add_compile_options(--sysroot=${CROSS_ROOTFS})

set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -target ${TOOLCHAIN}")
set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} --sysroot=${CROSS_ROOTFS}")

if("${CLI_CMAKE_RUNTIME_ID}" MATCHES "tizen.*")
    set(TIZEN_TOOLCHAIN "armv7l-tizen-linux-gnueabi/4.9.2")
    include_directories(${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/armv7l-tizen-linux-gnueabi)
    add_compile_options(-Wno-deprecated-declarations) # compile-time option
    add_compile_options(-D__extern_always_inline=inline)
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -B${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -L${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
else()
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -B${CROSS_ROOTFS}/usr/lib/gcc/${TOOLCHAIN}")
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -L${CROSS_ROOTFS}/usr/lib/gcc/${TOOLCHAIN}")
endif()

set(CMAKE_EXE_LINKER_FLAGS    "${CMAKE_EXE_LINKER_FLAGS}    ${CROSS_LINK_FLAGS}" CACHE STRING "" FORCE)
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} ${CROSS_LINK_FLAGS}" CACHE STRING "" FORCE)
set(CMAKE_MODULE_LINKER_FLAGS "${CMAKE_MODULE_LINKER_FLAGS} ${CROSS_LINK_FLAGS}" CACHE STRING "" FORCE)

set(CMAKE_FIND_ROOT_PATH "${CROSS_ROOTFS}")
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
