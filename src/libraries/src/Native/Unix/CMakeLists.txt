cmake_minimum_required(VERSION 2.8.12)
project(CoreFX)

set(CMAKE_MACOSX_RPATH ON)
set(CMAKE_INSTALL_PREFIX $ENV{__CMakeBinDir})
set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=gnu99")
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
set(CMAKE_SHARED_LIBRARY_PREFIX "")
set(VERSION_FILE_PATH "${CMAKE_BINARY_DIR}/../../version.c")

add_compile_options(-Wno-format-nonliteral)
add_compile_options(-Wno-missing-prototypes)
add_compile_options(-Wno-disabled-macro-expansion)
add_compile_options(-Wno-c++98-compat)
add_compile_options(-Wno-padded)
add_compile_options(-Wno-empty-translation-unit)
add_compile_options(-Wno-old-style-cast)
add_compile_options(-Wno-cast-align)
add_compile_options(-Wno-typedef-redefinition)
add_compile_options(-Wno-unused-local-typedef)
add_compile_options(-Wno-c99-extensions)
add_compile_options(-Wno-c11-extensions)
add_compile_options(-fPIC)
add_compile_options(-I${CMAKE_CURRENT_SOURCE_DIR}/Common)
add_compile_options(-I${CMAKE_CURRENT_BINARY_DIR}/Common)
add_compile_options(-g)
add_compile_options(-Werror)
if (CMAKE_CXX_COMPILER_VERSION VERSION_LESS 3.5)
    add_compile_options(-Wno-unreachable-code)
endif ()

if (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64)
    add_definitions(-DBIT64=1)
    add_definitions(-D_AMD64_)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL x86 OR CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
    add_definitions(-DBIT32=1)
    add_definitions(-D_X86_)
    add_definitions(-D_FILE_OFFSET_BITS=64)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
    add_definitions(-DBIT64=1)
    add_definitions(-D_ARM64_)
    # Because we don't use CMAKE_C_COMPILER/CMAKE_CXX_COMPILER to use clang
    # we have to set the triple by adding a compiler argument
    add_compile_options(-target aarch64-linux-gnu)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l)
    add_definitions(-DBIT32=1)
    add_definitions(-D_ARM_)
    add_definitions(-D_FILE_OFFSET_BITS=64)
    # Because we don't use CMAKE_C_COMPILER/CMAKE_CXX_COMPILER to use clang
    # we have to set the triple by adding a compiler argument
  if(ARM_SOFTFP)
    add_compile_options(-target armv7-linux-gnueabi)
    add_compile_options(-mfloat-abi=softfp)
  else ()
    add_compile_options(-target armv7-linux-gnueabihf)
  endif ()
    add_compile_options(-mthumb)
    add_compile_options(-mfpu=vfpv3)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
    add_definitions(-DBIT32=1)
    add_definitions(-D_ARM_)
    add_definitions(-D_FILE_OFFSET_BITS=64)
endif ()

string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    add_compile_options(-O0)
    add_definitions(-DDEBUG)

    # obtain settings from running coreclr\enablesanitizers.sh
    string(FIND "$ENV{DEBUG_SANITIZERS}" "asan" __ASAN_POS)
    string(FIND "$ENV{DEBUG_SANITIZERS}" "ubsan" __UBSAN_POS)
    if ((${__ASAN_POS} GREATER -1) OR (${__UBSAN_POS} GREATER -1))
      set(CLR_SANITIZE_CXX_FLAGS "${CLR_SANITIZE_CXX_FLAGS} -fsanitize=")
      set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS} -fsanitize=")
      if (${__ASAN_POS} GREATER -1) 
        set(CLR_SANITIZE_CXX_FLAGS "${CLR_SANITIZE_CXX_FLAGS}address,")
        set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}address,")
        message("Address Sanitizer (asan) enabled")
      endif ()
      if (${__UBSAN_POS} GREATER -1)
        set(CLR_SANITIZE_CXX_FLAGS "${CLR_SANITIZE_CXX_FLAGS}undefined")
        set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}undefined")
        message("Undefined Behavior Sanitizer (ubsan) enabled")
      endif ()

      # -fdata-sections -ffunction-sections: each function has own section instead of one per .o file (needed for --gc-sections)
      # -fPIC: enable Position Independent Code normally just for shared libraries but required when linking with address sanitizer
      set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} ${CLR_SANITIZE_CXX_FLAGS} -fdata-sections -ffunction-sections -fPIC")

      set(CMAKE_EXE_LINKER_FLAGS_DEBUG "${CMAKE_EXE_LINKER_FLAGS_DEBUG} ${CLR_SANITIZE_LINK_FLAGS}")

      # -Wl and --gc-sections: drop unused sections\functions (similar to Windows /Gy function-level-linking)
      set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG} ${CLR_SANITIZE_LINK_FLAGS} -Wl,--gc-sections")
    endif ()
elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
    # Use O1 option when the clang version is smaller than 3.9
    # Otherwise use O3 option in release build
    if (CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l AND DEFINED ENV{CROSSCOMPILE} AND CMAKE_CXX_COMPILER_VERSION VERSION_LESS 3.9)
        add_compile_options (-O1)
    else()
        add_compile_options (-O3)
    endif()
    add_definitions(-DNDEBUG)
else ()
    message(FATAL_ERROR "Unknown build type. Set CMAKE_BUILD_TYPE to DEBUG or RELEASE.")
endif ()

if (APPLE)
    add_definitions(-D__APPLE_USE_RFC_3542)
   
   # We cannot enable "stack-protector-strong" on OS X due to a bug in clang compiler (current version 7.0.2)  
   add_compile_options(-fstack-protector)  
else ()  
   add_compile_options(-fstack-protector-strong)  
endif ()

if (CMAKE_SYSTEM_NAME STREQUAL Linux)
   set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -D_GNU_SOURCE")
   set(CMAKE_ASM_FLAGS "${CMAKE_ASM_FLAGS} -Wa,--noexecstack")
   set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--build-id=sha1")
   set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,--build-id=sha1")
endif ()

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    add_definitions(-D_BSD_SOURCE) # required for getline
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
endif(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)

if(CMAKE_SYSTEM_NAME STREQUAL NetBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
endif(CMAKE_SYSTEM_NAME STREQUAL NetBSD)

if(CMAKE_SYSTEM_NAME STREQUAL SunOS)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
endif(CMAKE_SYSTEM_NAME STREQUAL SunOS)

if (NOT WIN32)
    if (CMAKE_SYSTEM_NAME STREQUAL Darwin)
        # Ensure that dsymutil and strip are present
        find_program(DSYMUTIL dsymutil)
        if (DSYMUTIL STREQUAL "DSYMUTIL-NOTFOUND")
            message(FATAL_ERROR "dsymutil not found")
        endif()

        find_program(STRIP strip)
        if (STRIP STREQUAL "STRIP-NOTFOUND")
            message(FATAL_ERROR "strip not found")
        endif()
    else (CMAKE_SYSTEM_NAME STREQUAL Darwin)
        # Ensure that objcopy is present
        if(DEFINED ENV{CROSSCOMPILE})
            if(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l OR CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL i686 OR CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
                find_program(OBJCOPY ${TOOLCHAIN}-objcopy)
            else()
                message(FATAL_ERROR "Only AMD64, X86, ARM64 and ARM are supported")
            endif()
        else()
            find_program(OBJCOPY objcopy)
        endif()
        if (OBJCOPY STREQUAL "OBJCOPY-NOTFOUND" AND NOT CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
            message(FATAL_ERROR "objcopy not found")
        endif()
    endif (CMAKE_SYSTEM_NAME STREQUAL Darwin)
endif ()


function(strip_symbols targetName outputFilename)
    if(CLR_CMAKE_PLATFORM_UNIX)
        if(STRIP_SYMBOLS)

            # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE
            # generator expression doesn't work correctly returning the wrong path and on
            # the newer cmake versions the LOCATION property isn't supported anymore.
            if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
                set(strip_source_file $<TARGET_FILE:${targetName}>)
            else()
                get_property(strip_source_file TARGET ${targetName} PROPERTY LOCATION)
            endif()

            if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
                set(strip_destination_file ${strip_source_file}.dwarf)

                add_custom_command(
                    TARGET ${targetName}
                    POST_BUILD
                    VERBATIM 
                    COMMAND ${DSYMUTIL} --flat --minimize ${strip_source_file}
                    COMMAND ${STRIP} -u -r ${strip_source_file}
                    COMMENT Stripping symbols from ${strip_source_file} into file ${strip_destination_file}
                )
            else(CMAKE_SYSTEM_NAME STREQUAL Darwin)
                set(strip_destination_file ${strip_source_file}.dbg)

                add_custom_command(
                    TARGET ${targetName}
                    POST_BUILD
                    VERBATIM 
                    COMMAND ${OBJCOPY} --only-keep-debug ${strip_source_file} ${strip_destination_file}
                    COMMAND ${OBJCOPY} --strip-unneeded ${strip_source_file}
                    COMMAND ${OBJCOPY} --add-gnu-debuglink=${strip_destination_file} ${strip_source_file}
                    COMMENT Stripping symbols from ${strip_source_file} into file ${strip_destination_file}
                )
            endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

            set(${outputFilename} ${strip_destination_file} PARENT_SCOPE)
        endif(STRIP_SYMBOLS)
    endif(CLR_CMAKE_PLATFORM_UNIX)
endfunction()

function(install_library_and_symbols targetName)
    strip_symbols(${targetName} strip_destination_file)

    # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE
    # generator expression doesn't work correctly returning the wrong path and on
    # the newer cmake versions the LOCATION property isn't supported anymore.
    if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
        set(install_source_file $<TARGET_FILE:${targetName}>)
    else()
        get_property(install_source_file TARGET ${targetName} PROPERTY LOCATION)
    endif()

    install(PROGRAMS ${install_source_file} DESTINATION .)
    if(WIN32)
        install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION PDB)
    else()
        install(FILES ${strip_destination_file} DESTINATION .)
    endif()
endfunction()

include(configure.cmake)

add_subdirectory(System.IO.Compression.Native)

add_compile_options(-Weverything)
add_compile_options(-Wno-c++98-compat-pedantic)

add_subdirectory(System.Native)
add_subdirectory(System.Net.Http.Native)
add_subdirectory(System.Net.Security.Native)
add_subdirectory(System.Security.Cryptography.Native)

if(APPLE)
    add_subdirectory(System.Security.Cryptography.Native.Apple)
endif()
