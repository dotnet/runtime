# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

set (CMAKE_CXX_STANDARD 11)

if (NOT CLR_CMAKE_HOST_WIN32)
    # Try to locate the paxctl tool. Failure to find it is not fatal,
    # but the generated executables won't work on a system where PAX is set
    # to prevent applications to create executable memory mappings.
    find_program(PAXCTL paxctl)

    if (CLR_CMAKE_HOST_DARWIN)
        # Ensure that dsymutil and strip are present
        find_program(DSYMUTIL dsymutil)
        if (DSYMUTIL STREQUAL "DSYMUTIL-NOTFOUND")
            message(FATAL_ERROR "dsymutil not found")
        endif()

        find_program(STRIP strip)
        if (STRIP STREQUAL "STRIP-NOTFOUND")
            message(FATAL_ERROR "strip not found")
        endif()
    endif (CLR_CMAKE_HOST_DARWIN)
endif ()

function(install_symbols targetName destination_path)
    if(CLR_CMAKE_TARGET_WIN32)
        install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION ${destination_path})
    else()
        strip_symbols(${targetName} strip_destination_file NO)
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

if(CLR_CMAKE_HOST_WIN32)
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
    set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG} /NOVCFEATURE")

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
    # Suppress warnings-as-errors in release branches to reduce servicing churn
    if (PRERELEASE)
        add_compile_options(-Werror)
    endif()
    add_compile_options(-Wno-missing-field-initializers)
    add_compile_options(-Wno-unused-function)
    add_compile_options(-Wno-unused-local-typedef)
    add_compile_options(-Wno-unused-macros)
    add_compile_options(-Wno-unused-parameter)

    if(CLR_CMAKE_TARGET_ARCH_ARM)
        if (NOT DEFINED CLR_ARM_FPU_TYPE)
            set(CLR_ARM_FPU_TYPE vfpv3)
        endif(NOT DEFINED CLR_ARM_FPU_TYPE)

        if (NOT DEFINED CLR_ARM_FPU_CAPABILITY)
            set(CLR_ARM_FPU_CAPABILITY 0x7)
        endif(NOT DEFINED CLR_ARM_FPU_CAPABILITY)

        add_definitions(-DCLR_ARM_FPU_CAPABILITY=${CLR_ARM_FPU_CAPABILITY})
    endif()
endif()

# Older CMake doesn't support CMAKE_CXX_STANDARD and GCC/Clang need a switch to enable C++ 11
if(${CMAKE_CXX_COMPILER_ID} MATCHES "(Clang|GNU)")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
endif()

# This is required to map a symbol reference to a matching definition local to the module (.so)
# containing the reference instead of using definitions from other modules.
if(CLR_CMAKE_TARGET_LINUX)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")
    add_link_options(-Wl,--build-id=sha1 -Wl,-z,relro,-z,now)
    add_compile_options(-fstack-protector-strong)
elseif(CLR_CMAKE_TARGET_DARWIN)
    add_compile_options(-fstack-protector)
elseif(CLR_CMAKE_TARGET_FREEBSD)
    add_link_options(-fuse-ld=lld -Wl,--build-id=sha1 -Wl,-z,relro,-z,now)
    add_compile_options(-fstack-protector)
endif()

if(CLR_CMAKE_TARGET_ANDROID)
    add_compile_options(-Wno-user-defined-warnings)
endif()

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
if(CLR_CMAKE_TARGET_LINUX)
    add_definitions(-D__LINUX__)
endif()

if(CLR_CMAKE_TARGET_ARCH_I386)
    add_definitions(-D_TARGET_X86_=1)
    set(ARCH_SPECIFIC_FOLDER_NAME "i386")
elseif(CLR_CMAKE_TARGET_ARCH_AMD64)
    add_definitions(-D_TARGET_AMD64_=1)
    set(ARCH_SPECIFIC_FOLDER_NAME "AMD64")
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
    add_definitions(-D_TARGET_ARM_=1)
    set(ARCH_SPECIFIC_FOLDER_NAME "arm")
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
    add_definitions(-D_TARGET_ARM64_=1)
    set(ARCH_SPECIFIC_FOLDER_NAME "arm64")
else()
    message(FATAL_ERROR "Unknown target architecture")
endif()

# Specify the Windows SDK to be used for Arm builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    if(NOT DEFINED CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION OR CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION STREQUAL "" )
	      message(FATAL_ERROR "Windows SDK is required for the Arm32 or Arm64 build.")
      else()
	      message("Using Windows SDK version ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}")
      endif()
endif ()

if (CLR_CMAKE_HOST_WIN32)
    if(CLR_CMAKE_HOST_ARCH_ARM)
      # Explicitly specify the assembler to be used for Arm32 compile
      file(TO_CMAKE_PATH "$ENV{VCToolsInstallDir}\\bin\\HostX86\\arm\\armasm.exe" CMAKE_ASM_COMPILER)

      set(CMAKE_ASM_MASM_COMPILER ${CMAKE_ASM_COMPILER})
      message("CMAKE_ASM_MASM_COMPILER explicitly set to: ${CMAKE_ASM_MASM_COMPILER}")

      # Enable generic assembly compilation to avoid CMake generate VS proj files that explicitly
      # use ml[64].exe as the assembler.
      enable_language(ASM)
    elseif(CLR_CMAKE_HOST_ARCH_ARM64)
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
