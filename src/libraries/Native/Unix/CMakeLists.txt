cmake_minimum_required(VERSION 3.14.2)
cmake_policy(SET CMP0042 NEW)

project(CoreFX C)

include(${CLR_ENG_NATIVE_DIR}/configuretools.cmake)

if(CLR_CMAKE_TARGET_IOS)
    cmake_minimum_required(VERSION 3.14.5)
endif()

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
    cmake_policy(SET CMP0083 NEW)
endif(NOT CLR_CMAKE_TARGET_ARCH_WASM)

set(CMAKE_MACOSX_RPATH ON)
set(CMAKE_INSTALL_PREFIX $ENV{__CMakeBinDir})
set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=gnu99")
set(VERSION_FILE_PATH "${CMAKE_BINARY_DIR}/version.c")

# We mark the function which needs exporting with PALEXPORT
add_compile_options(-fvisibility=hidden)

add_compile_options(-Wno-format-nonliteral)
add_compile_options(-Wno-disabled-macro-expansion)
add_compile_options(-Wno-padded)
add_compile_options(-Wno-empty-translation-unit)
add_compile_options(-Wno-cast-align)
add_compile_options(-Wno-typedef-redefinition)
add_compile_options(-Wno-c11-extensions)
add_compile_options(-I${CMAKE_CURRENT_SOURCE_DIR}/Common)
add_compile_options(-I${CMAKE_CURRENT_BINARY_DIR}/Common)
add_compile_options(-g)
if(CMAKE_C_COMPILER_ID STREQUAL Clang)
    add_compile_options(-Wthread-safety)
    add_compile_options(-Wno-thread-safety-analysis)
endif()

# Suppress warnings-as-errors in release branches to reduce servicing churn
if (PRERELEASE)
    add_compile_options(-Werror)
endif()

if(CLR_CMAKE_TARGET_ARCH_WASM)
    add_definitions(-D_WASM_)
    # The emscripten build has additional warnings so -Werror breaks
    add_compile_options(-Wno-unused-parameter)
    add_compile_options(-Wno-unused-function)
    add_compile_options(-Wno-alloca)
    add_compile_options(-Wno-implicit-int-float-conversion)
else()
  set(GEN_SHARED_LIB 1)
endif(CLR_CMAKE_TARGET_ARCH_WASM)

if (CLR_CMAKE_TARGET_ARCH_AMD64)
    add_definitions(-DTARGET_64BIT=1)
    add_definitions(-DTARGET_AMD64)
elseif (CLR_CMAKE_TARGET_ARCH_I386)
    add_definitions(-DTARGET_32BIT=1)
    add_definitions(-DTARGET_X86)
    add_definitions(-D_FILE_OFFSET_BITS=64)
elseif (CLR_CMAKE_TARGET_ARCH_ARM64)
    add_definitions(-DTARGET_64BIT=1)
    add_definitions(-DTARGET_ARM64)
elseif (CLR_CMAKE_TARGET_ARCH_ARM)
    add_definitions(-DTARGET_32BIT=1)
    add_definitions(-DTARGET_ARM)
    add_definitions(-D_FILE_OFFSET_BITS=64)

    if (CLR_CMAKE_TARGET_ARCH_ARMV7L)
        if(ARM_SOFTFP)
            add_compile_options(-mfloat-abi=softfp)
        endif ()

        add_compile_options(-mthumb)
        if (NOT DEFINED CLR_ARM_FPU_TYPE)
            set(CLR_ARM_FPU_TYPE vfpv3)
        endif(NOT DEFINED CLR_ARM_FPU_TYPE)

        add_compile_options(-mfpu=${CLR_ARM_FPU_TYPE})
        if (NOT DEFINED CLR_ARM_FPU_CAPABILITY)
            set(CLR_ARM_FPU_CAPABILITY 0x7)
        endif(NOT DEFINED CLR_ARM_FPU_CAPABILITY)

        add_definitions(-DCLR_ARM_FPU_CAPABILITY=${CLR_ARM_FPU_CAPABILITY})
        add_compile_options(-march=armv7-a)
    endif()
endif ()

if(CLR_CMAKE_TARGET_ANDROID)
    add_definitions(-DTARGET_ANDROID)
    include_directories(SYSTEM "${CROSS_ROOTFS}/usr/include")
endif()

string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    add_compile_options(-O0)
    add_definitions(-DDEBUG)

    # obtain settings from running coreclr\enablesanitizers.sh
    string(FIND "$ENV{DEBUG_SANITIZERS}" "asan" __ASAN_POS)
    string(FIND "$ENV{DEBUG_SANITIZERS}" "ubsan" __UBSAN_POS)
    if ((${__ASAN_POS} GREATER -1) OR (${__UBSAN_POS} GREATER -1))
      set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS} -fsanitize=")
      if (${__ASAN_POS} GREATER -1)
        set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}address,")
        message("Address Sanitizer (asan) enabled")
      endif ()
      if (${__UBSAN_POS} GREATER -1)
        set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}undefined")
        message("Undefined Behavior Sanitizer (ubsan) enabled")
      endif ()

      set(CMAKE_EXE_LINKER_FLAGS_DEBUG "${CMAKE_EXE_LINKER_FLAGS_DEBUG} ${CLR_SANITIZE_LINK_FLAGS}")

      # -Wl and --gc-sections: drop unused sections\functions (similar to Windows /Gy function-level-linking)
      set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG} ${CLR_SANITIZE_LINK_FLAGS} -Wl,--gc-sections")
    endif ()
elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
    # Use O1 option when the clang version is smaller than 3.9
    # Otherwise use O3 option in release build
    if (CLR_CMAKE_TARGET_ARCH_ARMV7L AND DEFINED ENV{CROSSCOMPILE} AND CMAKE_C_COMPILER_VERSION VERSION_LESS 3.9)
        add_compile_options (-O1)
    else()
        add_compile_options (-O3)
    endif()
    add_definitions(-DNDEBUG)
else ()
    message(FATAL_ERROR "Unknown build type. Set CMAKE_BUILD_TYPE to DEBUG or RELEASE.")
endif ()

if(CLR_CMAKE_TARGET_ARCH_WASM)
elseif (CLR_CMAKE_TARGET_DARWIN OR CLR_CMAKE_TARGET_IOS)
    add_definitions(-D__APPLE_USE_RFC_3542)

   # We cannot enable "stack-protector-strong" on OS X due to a bug in clang compiler (current version 7.0.2)
   add_compile_options(-fstack-protector)
else ()
   add_compile_options(-fstack-protector-strong)
endif ()

if (CLR_CMAKE_TARGET_LINUX)
   set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -D_GNU_SOURCE")
endif ()

if(CLR_CMAKE_TARGET_FREEBSD)
    add_definitions(-D_BSD_SOURCE) # required for getline
    add_link_options(-fuse-ld=lld)
endif(CLR_CMAKE_TARGET_FREEBSD)

# CLR_ADDITIONAL_LINKER_FLAGS - used for passing additional arguments to linker
# CLR_ADDITIONAL_COMPILER_OPTIONS - used for passing additional arguments to compiler
#
# For example:
#       ./build-native.sh cmakeargs -DCLR_ADDITIONAL_COMPILER_OPTIONS=<...> cmakeargs -DCLR_ADDITIONAL_LINKER_FLAGS=<...>
#
if(CLR_CMAKE_TARGET_UNIX)
    if (CLR_CMAKE_TARGET_DARWIN)
        add_link_options(-Wl,-bind_at_load)

        # Ensure that dsymutil and strip are present
        find_program(DSYMUTIL dsymutil)
        if (DSYMUTIL STREQUAL "DSYMUTIL-NOTFOUND")
            message(FATAL_ERROR "dsymutil not found")
        endif()

        find_program(STRIP strip)
        if (STRIP STREQUAL "STRIP-NOTFOUND")
            message(FATAL_ERROR "strip not found")
        endif()
    elseif (CLR_CMAKE_TARGET_IOS)
        # No object stripping for iOS
    elseif (CLR_CMAKE_TARGET_ARCH_WASM)
        # No object stripping for WASM
    else (CLR_CMAKE_TARGET_DARWIN)
        add_compile_options($<$<COMPILE_LANGUAGE:ASM>:-Wa,--noexecstack>)
        add_link_options(-Wl,--build-id=sha1 -Wl,-z,relro,-z,now)
    endif(CLR_CMAKE_TARGET_DARWIN)

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} ${CLR_ADDITIONAL_LINKER_FLAGS}")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${CLR_ADDITIONAL_LINKER_FLAGS}" )
    add_compile_options(${CLR_ADDITIONAL_COMPILER_OPTIONS})
endif(CLR_CMAKE_TARGET_UNIX)

function(install_library_and_symbols targetName)
    strip_symbols(${targetName} strip_destination_file NO)

    # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE
    # generator expression doesn't work correctly returning the wrong path and on
    # the newer cmake versions the LOCATION property isn't supported anymore.
    if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
        set(install_source_file $<TARGET_FILE:${targetName}>)
    else()
        get_property(install_source_file TARGET ${targetName} PROPERTY LOCATION)
    endif()

    install(PROGRAMS ${install_source_file} DESTINATION .)
    if(CLR_CMAKE_TARGET_WIN32)
        install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION PDB)
    else()
        install(FILES ${strip_destination_file} DESTINATION .)
    endif()
endfunction()

include(configure.cmake)

add_subdirectory(System.IO.Compression.Native)

if (NOT CLR_CMAKE_TARGET_ARCH_WASM)
if (NOT CLR_CMAKE_TARGET_IOS)  # TODO: reenable
    add_subdirectory(System.IO.Ports.Native)
endif()
endif()

if(CMAKE_C_COMPILER_ID STREQUAL Clang)
    add_compile_options(-Weverything)
endif()

add_subdirectory(System.Native)

if (NOT CLR_CMAKE_TARGET_ARCH_WASM)
if (NOT CLR_CMAKE_TARGET_IOS)  # TODO: reenable
    add_subdirectory(System.Globalization.Native)
    add_subdirectory(System.Net.Security.Native)
endif()
if (NOT CLR_CMAKE_TARGET_IOS)
    # disable System.Security.Cryptography.Native build on iOS,
    # only used for interacting with OpenSSL which isn't useful there
    add_subdirectory(System.Security.Cryptography.Native)
endif()
endif()

if(CLR_CMAKE_TARGET_DARWIN OR CLR_CMAKE_TARGET_IOS)
    add_subdirectory(System.Security.Cryptography.Native.Apple)
endif()
