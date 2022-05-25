cmake_minimum_required(VERSION 3.6.2)
include(CheckCCompilerFlag)

if (CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    # CMake 3.14.5 contains bug fixes for iOS
    cmake_minimum_required(VERSION 3.14.5)
elseif (CLR_CMAKE_TARGET_MACCATALYST)
    # CMake 3.18.1 properly generates MacCatalyst C compiler
    cmake_minimum_required(VERSION 3.18.1)
endif ()

if (WIN32)
    cmake_policy(SET CMP0091 NEW)
else ()
    cmake_policy(SET CMP0042 NEW)
endif ()

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

if (CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_BROWSER)
    set(CMAKE_MACOSX_RPATH ON)
    if (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
        set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
        set(CMAKE_INSTALL_NAME_DIR "@rpath")
    endif ()

    set(CMAKE_INSTALL_PREFIX $ENV{__CMakeBinDir})
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=gnu99 -Wno-declaration-after-statement")

    add_compile_options(-I${CMAKE_CURRENT_SOURCE_DIR}/Common)
    add_compile_options(-I${CMAKE_CURRENT_BINARY_DIR}/Common)

    if (CLR_CMAKE_TARGET_BROWSER)
        set(GEN_SHARED_LIB 0)
        set(STATIC_LIB_DESTINATION .)
    endif ()

    if (CLR_CMAKE_TARGET_TVOS)
        # with -fembed-bitcode passing -headerpad_max_install_names is not allowed so remove it from the CMake flags
        string(REPLACE "-Wl,-headerpad_max_install_names" "" CMAKE_C_LINK_FLAGS ${CMAKE_C_LINK_FLAGS})
        string(REPLACE "-Wl,-headerpad_max_install_names" "" CMAKE_SHARED_LIBRARY_CREATE_C_FLAGS ${CMAKE_SHARED_LIBRARY_CREATE_C_FLAGS})
        add_compile_options(-fembed-bitcode)
        add_link_options(-fembed-bitcode)
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

        # obtain settings from running coreclr\enablesanitizers.sh
        string(FIND "$ENV{DEBUG_SANITIZERS}" "asan" __ASAN_POS)
        string(FIND "$ENV{DEBUG_SANITIZERS}" "ubsan" __UBSAN_POS)
        if (${__ASAN_POS} GREATER -1 OR ${__UBSAN_POS} GREATER -1)
            set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS} -fsanitize=")
            if (${__ASAN_POS} GREATER -1)
                set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}address,")
                message("Address Sanitizer (asan) enabled")
            endif ()
            if (${__UBSAN_POS} GREATER -1)
                set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}undefined")
                message("Undefined Behavior Sanitizer (ubsan) enabled")
            endif ()

            set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${CLR_SANITIZE_LINK_FLAGS}")

            # -Wl and --gc-sections: drop unused sections\functions (similar to Windows /Gy function-level-linking)
            set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} ${CLR_SANITIZE_LINK_FLAGS} -Wl,--gc-sections")
        endif ()
    elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
        # Use O1 option when the clang version is smaller than 3.9
        # Otherwise use O3 option in release build
        if (CLR_CMAKE_TARGET_ARCH_ARMV7L AND DEFINED ENV{CROSSCOMPILE} AND CMAKE_C_COMPILER_VERSION VERSION_LESS 3.9)
            add_compile_options (-O1)
        else ()
            add_compile_options (-O3)
        endif ()
    else ()
        message(FATAL_ERROR "Unknown build type. Set CMAKE_BUILD_TYPE to DEBUG or RELEASE.")
    endif ()

    if (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
        add_definitions(-D__APPLE_USE_RFC_3542)
    endif ()

    if (CLR_CMAKE_TARGET_LINUX)
        set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -D_GNU_SOURCE")
    endif ()
else ()
    set(CMAKE_SHARED_LIBRARY_PREFIX "")

    # we only need to build System.Globalization.Native when building static libs.
    if (STATIC_LIBS_ONLY)
        add_subdirectory(System.Globalization.Native)
    endif ()
endif ()

add_subdirectory(System.IO.Compression.Native)

if (CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_BROWSER)
    include(configure.cmake)

    if (NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID)
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
    endif ()

    add_subdirectory(System.Native)

    if (CLR_CMAKE_TARGET_BROWSER)
        add_subdirectory(System.Security.Cryptography.Native.Browser)
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
        add_subdirectory(System.Globalization.Native)
        add_subdirectory(System.Net.Security.Native)
        add_subdirectory(System.Security.Cryptography.Native)
    endif ()

    if (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
        add_subdirectory(System.Security.Cryptography.Native.Apple)
    endif ()
endif ()
