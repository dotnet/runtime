# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

set (CMAKE_CXX_STANDARD 11)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name Linux")
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name Darwin")
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    add_definitions(-D_BSD_SOURCE) # required for getline
    message("System name FreeBSD")
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name OpenBSD")
endif(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)

if(CMAKE_SYSTEM_NAME STREQUAL NetBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name NetBSD")
endif(CMAKE_SYSTEM_NAME STREQUAL NetBSD)

if(CMAKE_SYSTEM_NAME STREQUAL SunOS)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name SunOS")
endif(CMAKE_SYSTEM_NAME STREQUAL SunOS)

if (NOT WIN32)
    # Try to locate the paxctl tool. Failure to find it is not fatal,
    # but the generated executables won't work on a system where PAX is set
    # to prevent applications to create executable memory mappings.
    find_program(PAXCTL paxctl)

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
        if(DEFINED ENV{ROOTFS_DIR})
            if(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l OR CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
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

function(install_symbols targetName destination_path)
    if(WIN32)
        install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION ${destination_path})
    else()
        strip_symbols(${targetName} strip_destination_file)
        install(FILES ${strip_destination_file} DESTINATION ${destination_path})
    endif()
endfunction()

# Disable PAX mprotect that would prevent JIT and other codegen in coreclr from working.
# PAX mprotect prevents:
# - changing the executable status of memory pages that were
#   not originally created as executable,
# - making read-only executable pages writable again,
# - creating executable pages from anonymous memory,
# - making read-only-after-relocations (RELRO) data pages writable again.
function(disable_pax_mprotect targetName)
    if (NOT PAXCTL STREQUAL "PAXCTL-NOTFOUND")
        add_custom_command(
            TARGET ${targetName}
            POST_BUILD
            VERBATIM
            COMMAND ${PAXCTL} -c -m $<TARGET_FILE:${targetName}>
        )
    endif()
endfunction()

if(WIN32)
    add_definitions(-DWIN32)
    add_definitions(-D_WIN32=1)
    if(IS_64BIT_BUILD)
        add_definitions(-D_WIN64=1)
    endif()
    add_compile_options($<$<CONFIG:Debug>:-DDEBUG>)
    add_compile_options($<$<CONFIG:Release>:-DNDEBUG>)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:-DNDEBUG>)
    add_compile_options($<$<CONFIG:Debug>:/Od>)
    add_compile_options(/guard:cf) 
    add_compile_options(/d2Zi+) # make optimized builds debugging easier
    add_compile_options(/Oi) # enable intrinsics
    add_compile_options(/Oy-) # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
    add_compile_options(/GF) # enable read-only string pooling
    add_compile_options(/FC) # use full pathnames in diagnostics
    add_compile_options(/DEBUG)
    add_compile_options(/Zi) # enable debugging information
    add_compile_options(/GS)
    add_compile_options(/W1)
    add_compile_options(/we5038) # make reorder warnings into errors
    add_compile_options(/Zc:inline)
    add_compile_options(/fp:precise)
    add_compile_options(/EHsc)

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /LARGEADDRESSAWARE") # can handle addresses larger than 2 gigabytes
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /INCREMENTAL:NO")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /PDBCOMPRESS")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:1572864")

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /GUARD:CF")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /GUARD:CF")

    # Debug build specific flags
    set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "/NOVCFEATURE")

    # Release build specific flags
    set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")
    set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")

    # RelWithDebInfo specific flags
    set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")
    set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")
else()
    add_compile_options(-g) # enable debugging information
    add_compile_options(-Wall)
    add_compile_options(-Wextra)
    if(CMAKE_C_COMPILER_ID STREQUAL Clang)
        # Uncomment to enable additional, but likely irrelvant, warnings. For
        # example, this will warn about using c++11 features even when
        # compiling with -std=c++11.
        # add_compile_options(-Weverything)
    endif()
    add_compile_options(-Werror)
    add_compile_options(-Wno-missing-field-initializers)
    add_compile_options(-Wno-unused-function)
    add_compile_options(-Wno-unused-local-typedef)
    add_compile_options(-Wno-unused-macros)
    add_compile_options(-Wno-unused-parameter)
endif()

# Older CMake doesn't support CMAKE_CXX_STANDARD and GCC/Clang need a switch to enable C++ 11
if(${CMAKE_CXX_COMPILER_ID} MATCHES "(Clang|GNU)")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
endif()

# This is required to map a symbol reference to a matching definition local to the module (.so)
# containing the reference instead of using definitions from other modules.
if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--build-id=sha1")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,--build-id=sha1")
    add_compile_options(-fstack-protector-strong)
elseif(${CMAKE_SYSTEM_NAME} MATCHES "Darwin")
    add_compile_options(-fstack-protector)
elseif(${CMAKE_SYSTEM_NAME} MATCHES "FreeBSD")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -fuse-ld=lld -Xlinker --build-id=sha1")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS}  -fuse-ld=lld -Xlinker --build-id=sha1")
    add_compile_options(-fstack-protector)
endif()

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    add_definitions(-D__LINUX__)
endif()

if(CLI_CMAKE_PLATFORM_ARCH_I386)
    add_definitions(-D_TARGET_X86_=1)
    set(ARCH_SPECIFIC_FOLDER_NAME "i386")
elseif(CLI_CMAKE_PLATFORM_ARCH_AMD64)
    add_definitions(-D_TARGET_AMD64_=1)
    set(ARCH_SPECIFIC_FOLDER_NAME "AMD64")
elseif(CLI_CMAKE_PLATFORM_ARCH_ARM)
    add_definitions(-D_TARGET_ARM_=1)
    set(ARCH_SPECIFIC_FOLDER_NAME "arm")
elseif(CLI_CMAKE_PLATFORM_ARCH_ARM64)
    add_definitions(-D_TARGET_ARM64_=1) 
    set(ARCH_SPECIFIC_FOLDER_NAME "arm64")
else()
    message(FATAL_ERROR "Unknown target architecture")
endif()

# Specify the Windows SDK to be used for Arm builds
if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))
    if(NOT DEFINED CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION OR CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION STREQUAL "" )
	      message(FATAL_ERROR "Windows SDK is required for the Arm32 or Arm64 build.")
      else()
	      message("Using Windows SDK version ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}")
      endif()
endif ()

if (WIN32)
    if(CLI_CMAKE_PLATFORM_ARCH_ARM)
      # Explicitly specify the assembler to be used for Arm32 compile
      file(TO_CMAKE_PATH "$ENV{VCToolsInstallDir}\\bin\\HostX86\\arm\\armasm.exe" CMAKE_ASM_COMPILER)

      set(CMAKE_ASM_MASM_COMPILER ${CMAKE_ASM_COMPILER})
      message("CMAKE_ASM_MASM_COMPILER explicitly set to: ${CMAKE_ASM_MASM_COMPILER}")

      # Enable generic assembly compilation to avoid CMake generate VS proj files that explicitly
      # use ml[64].exe as the assembler.
      enable_language(ASM)
    elseif(CLI_CMAKE_PLATFORM_ARCH_ARM64)
      # Explicitly specify the assembler to be used for Arm64 compile
      file(TO_CMAKE_PATH "$ENV{VCToolsInstallDir}\\bin\\HostX86\\arm64\\armasm64.exe" CMAKE_ASM_COMPILER)

      set(CMAKE_ASM_MASM_COMPILER ${CMAKE_ASM_COMPILER})
      message("CMAKE_ASM_MASM_COMPILER explicitly set to: ${CMAKE_ASM_MASM_COMPILER}")

      # Enable generic assembly compilation to avoid CMake generate VS proj files that explicitly
      # use ml[64].exe as the assembler.
      enable_language(ASM)
    else()
      enable_language(ASM_MASM)
    endif()
endif()
