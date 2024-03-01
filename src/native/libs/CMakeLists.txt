cmake_minimum_required(VERSION 3.20)

include(CheckCCompilerFlag)

project(CoreFX C)

include(../../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

include_directories(${CLR_SRC_NATIVE_DIR})

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if (STATIC_LIBS_ONLY)
    # Suppress exporting of the PAL APIs
    add_definitions(-DPALEXPORT=EXTERN_C)

    set(GEN_SHARED_LIB 0)
    set(STATIC_LIB_DESTINATION lib)
else ()
    set(GEN_SHARED_LIB 1)
    set(STATIC_LIB_DESTINATION .)
endif ()

if (CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
    set(CMAKE_MACOSX_RPATH ON)
    if (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
        set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
        set(CMAKE_INSTALL_NAME_DIR "@rpath")
    endif ()

    set(CMAKE_INSTALL_PREFIX $ENV{__CMakeBinDir})
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=gnu99 -Wno-declaration-after-statement")

    add_compile_options(-I${CMAKE_CURRENT_SOURCE_DIR}/Common)
    add_compile_options(-I${CMAKE_CURRENT_BINARY_DIR}/Common)

    if (CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
        set(GEN_SHARED_LIB 0)
        set(STATIC_LIB_DESTINATION .)
    endif ()

    if (CLR_CMAKE_TARGET_WASI)
        set(HOST_WASI 1)
        add_compile_options(-I${CLR_REPO_ROOT_DIR}/src/mono/wasi/include/)
        add_compile_options(-I${CLR_REPO_ROOT_DIR}/src/mono/wasi/libs-include/)
        add_compile_options(-Wno-unused-variable)
        add_compile_options(-Wno-unused-parameter)
        add_compile_options(-Wno-gnu-statement-expression)
        add_compile_options(-DHOST_WASI)
        add_compile_options(-D_WASI_EMULATED_PROCESS_CLOCKS)
        add_compile_options(-D_WASI_EMULATED_SIGNAL)
        add_compile_options(-D_WASI_EMULATED_MMAN)
        add_link_options(-Wl,-z,stack-size=1048576,--initial-memory=5242880,--max-memory=52428800,-lwasi-emulated-process-clocks,-lwasi-emulated-signal,-lwasi-emulated-mman)
    endif ()

    if (CLR_CMAKE_TARGET_ANDROID)
        if (CROSS_ROOTFS)
            include_directories(SYSTEM "${CROSS_ROOTFS}/usr/include")
        endif ()
    endif ()

    string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)

    if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
        if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
            add_compile_options(-O0)
        elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
            add_compile_options(-O2)
        endif ()

        add_definitions(-DDEBUG)
    elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
        # Use O1 option when the clang version is smaller than 3.9
        # Otherwise use O3 option in release build
        if (CLR_CMAKE_TARGET_ARCH_ARMV7L AND DEFINED ENV{CROSSCOMPILE} AND CMAKE_C_COMPILER_VERSION VERSION_LESS 3.9)
            add_compile_options (-O1)
        else ()
            if(CLR_CMAKE_TARGET_ANDROID)
                # -O2 optimization generates faster/smaller code on Android
                # TODO: This duplicates the settings in eng/native/configureoptimization.cmake, we should unify it
                add_compile_options (-O2)
            else()
                add_compile_options (-O3)
            endif ()
        endif ()
    else ()
        message(FATAL_ERROR "Unknown build type. Set CMAKE_BUILD_TYPE to DEBUG or RELEASE.")
    endif ()

    if (CLR_CMAKE_TARGET_APPLE)
        add_definitions(-D__APPLE_USE_RFC_3542)
    endif ()

    if (CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_HAIKU)
        set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -D_GNU_SOURCE")
    endif ()
else ()
    set(CMAKE_SHARED_LIBRARY_PREFIX "")
    add_subdirectory(System.Globalization.Native)
endif ()

add_subdirectory(System.IO.Compression.Native)

if (CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
    include(configure.cmake)

    if (NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
        add_subdirectory(System.IO.Ports.Native)
    endif ()

    if (CMAKE_C_COMPILER_ID STREQUAL Clang)
        add_compile_options(-Weverything)
        add_compile_options(-Wno-format-nonliteral)
        add_compile_options(-Wno-disabled-macro-expansion)
        add_compile_options(-Wno-padded)
        add_compile_options(-Wno-empty-translation-unit)
        add_compile_options(-Wno-cast-align)
        add_compile_options(-Wno-typedef-redefinition)
        add_compile_options(-Wno-c11-extensions)
        add_compile_options(-Wno-thread-safety-analysis)
        if (CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
            add_compile_options(-Wno-unsafe-buffer-usage)
            add_compile_options(-Wno-cast-function-type-strict)
        endif ()
    endif ()

    add_subdirectory(System.Native)
    add_subdirectory(System.Globalization.Native)

    if (CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
        # skip for now
    elseif (CLR_CMAKE_TARGET_MACCATALYST)
        add_subdirectory(System.Net.Security.Native)
        # System.Security.Cryptography.Native is intentionally disabled on iOS
        # it is only used for interacting with OpenSSL which isn't useful there
    elseif (CLR_CMAKE_TARGET_IOS)
        add_subdirectory(System.Net.Security.Native)
        # System.Security.Cryptography.Native is intentionally disabled on iOS
        # it is only used for interacting with OpenSSL which isn't useful there
    elseif (CLR_CMAKE_TARGET_TVOS)
        #add_subdirectory(System.Net.Security.Native) # no gssapi on tvOS, see https://developer.apple.com/documentation/gss
        # System.Security.Cryptography.Native is intentionally disabled on tvOS
        # it is only used for interacting with OpenSSL which isn't useful there
    elseif (CLR_CMAKE_TARGET_ANDROID AND NOT FORCE_ANDROID_OPENSSL)
        add_subdirectory(System.Security.Cryptography.Native.Android)
    elseif (FORCE_ANDROID_OPENSSL)
        add_subdirectory(System.Security.Cryptography.Native)
    else ()
        add_subdirectory(System.Net.Security.Native)
        add_subdirectory(System.Security.Cryptography.Native)
    endif ()

    if (CLR_CMAKE_TARGET_APPLE)
        add_subdirectory(System.Security.Cryptography.Native.Apple)
    endif ()
endif ()
