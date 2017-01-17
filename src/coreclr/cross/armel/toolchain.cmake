set(CROSS_ROOTFS $ENV{ROOTFS_DIR})

set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_VERSION 1)
set(CMAKE_SYSTEM_PROCESSOR armv7l)

## Specify the toolchain
set(TOOLCHAIN "arm-linux-gnueabi")
set(TOOLCHAIN_PREFIX ${TOOLCHAIN}-)
#set(CMAKE_C_COMPILER ${TOOLCHAIN_PREFIX}gcc)
#set(CMAKE_CXX_COMPILER ${TOOLCHAIN_PREFIX}g++)
#set(CMAKE_ASM_COMPILER ${TOOLCHAIN_PREFIX}as)
#set(CMAKE_AR ${TOOLCHAIN_PREFIX}ar)
#set(CMAKE_OBJCOPY ${TOOLCHAIN_PREFIX}objcopy)
#set(CMAKE_OBJDUMP ${TOOLCHAIN_PREFIX}objdump)

add_compile_options(-target armv7-linux-gnueabi)
add_compile_options(-mthumb)
add_compile_options(-mfpu=vfpv3)
add_compile_options(--sysroot=${CROSS_ROOTFS})

set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -target ${TOOLCHAIN}")
set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} --sysroot=${CROSS_ROOTFS}")

if("$ENV{__DistroRid}" MATCHES "tizen.*")
    add_compile_options(-I$ENV{ROOTFS_DIR}/usr/lib/gcc/armv7l-tizen-linux-gnueabi/4.9.2/include/c++/)
    add_compile_options(-I$ENV{ROOTFS_DIR}/usr/lib/gcc/armv7l-tizen-linux-gnueabi/4.9.2/include/c++/armv7l-tizen-linux-gnueabi)
    add_compile_options(-Wno-deprecated-declarations) # compile-time option
    add_compile_options(-D__extern_always_inline=inline)
    set(TIZEN_TOOLCHAIN "armv7l-tizen-linux-gnueabi/4.9.2")
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -B${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -L${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
else()
    # TODO: this setting assumes debian armel rootfs
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/include/c++/4.9 ${CROSS_ROOTFS}/usr/include/${TOOLCHAIN}/c++/4.9 )   
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -B${CROSS_ROOTFS}/usr/lib/gcc/${TOOLCHAIN}/4.9")
    set(CROSS_LINK_FLAGS "${CROSS_LINK_FLAGS} -L${CROSS_ROOTFS}/usr/lib/gcc/${TOOLCHAIN}/4.9")
endif()

set(CMAKE_EXE_LINKER_FLAGS    "${CMAKE_EXE_LINKER_FLAGS}    ${CROSS_LINK_FLAGS}" CACHE STRING "" FORCE)
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} ${CROSS_LINK_FLAGS}" CACHE STRING "" FORCE)
set(CMAKE_MODULE_LINKER_FLAGS "${CMAKE_MODULE_LINKER_FLAGS} ${CROSS_LINK_FLAGS}" CACHE STRING "" FORCE)

set(CMAKE_FIND_ROOT_PATH "${CROSS_ROOTFS}")
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)

set(LLVM_ARM_DIR "$ENV{LLVM_ARM_HOME}")
if(LLVM_ARM_DIR)
    set(WITH_LLDB_LIBS "${LLVM_ARM_DIR}/lib/" CACHE STRING "")
    set(WITH_LLDB_INCLUDES "${LLVM_ARM_DIR}/include" CACHE STRING "")
    set(LLDB_H "${WITH_LLDB_INCLUDES}" CACHE STRING "")
    set(LLDB "${LLVM_ARM_DIR}/lib/liblldb.so" CACHE STRING "")
else()
    set(WITH_LLDB_LIBS "${CROSS_ROOTFS}/usr/lib/${TOOLCHAIN}" CACHE STRING "")
    set(WITH_LLDB_INCLUDES "${CROSS_ROOTFS}/usr/lib/llvm-3.6/include" CACHE STRING "")
endif()
