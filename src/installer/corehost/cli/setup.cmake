# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set (CMAKE_CXX_STANDARD 11)

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
    add_compile_options(/GS)
    add_compile_options(/W1)
    add_compile_options(/Zc:inline)
    add_compile_options(/fp:precise)
    add_compile_options(/EHsc)

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /INCREMENTAL:NO")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /PDBCOMPRESS")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:1572864")

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
    add_compile_options(-Wno-unused-local-typedef)
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
endif()

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    add_definitions(-D__LINUX__)
endif()

if(CLI_CMAKE_PLATFORM_ARCH_I386)
    add_definitions(-D_TARGET_X86_=1)
elseif(CLI_CMAKE_PLATFORM_ARCH_AMD64)
    add_definitions(-D_TARGET_AMD64_=1)
elseif(CLI_CMAKE_PLATFORM_ARCH_ARM)
    add_definitions(-D_TARGET_ARM_=1)
elseif(CLI_CMAKE_PLATFORM_ARCH_ARM64)
    add_definitions(-D_TARGET_ARM64_=1) 
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /MACHINE:arm64")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /MACHINE:arm64")
else()
    message(FATAL_ERROR "Unknown target architecture")
endif()

# Specify the Windows SDK to be used for Arm builds
if (WIN32 AND CLI_CMAKE_PLATFORM_ARCH_ARM)
    if(NOT DEFINED CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION OR CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION STREQUAL "" )
	      message(FATAL_ERROR "Windows SDK is required for the Arm32 build.")
      else()
	      message("Using Windows SDK version ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}")
      endif()
endif ()

if("${CLI_CMAKE_RUNTIME_ID}" STREQUAL "")
    message(FATAL_ERROR "Runtime ID not specified")
else()
    add_definitions(-DTARGET_RUNTIME_ID="${CLI_CMAKE_RUNTIME_ID}")
endif()

if("${CLI_CMAKE_HOST_POLICY_VER}" STREQUAL "")
    message(FATAL_ERROR "Host policy version is not specified")
else()
    add_definitions(-DHOST_POLICY_PKG_VER="${CLI_CMAKE_HOST_POLICY_VER}")
endif()

if("${CLI_CMAKE_HOST_FXR_VER}" STREQUAL "")
    message(FATAL_ERROR "Host FXR version is not specified")
else()
    add_definitions(-DHOST_FXR_PKG_VER="${CLI_CMAKE_HOST_FXR_VER}")
endif()

if("${CLI_CMAKE_HOST_VER}" STREQUAL "")
    message(FATAL_ERROR "Dotnet host version is not specified")
else()
    add_definitions(-DHOST_PKG_VER="${CLI_CMAKE_HOST_VER}")
endif()

if("${CLI_CMAKE_APPHOST_VER}" STREQUAL "")
    message(FATAL_ERROR "apphost version is not specified")
else()
    add_definitions(-DAPPHOST_PKG_VER="${CLI_CMAKE_APPHOST_VER}")
endif()

if("${CLI_CMAKE_PKG_RID}" STREQUAL "")
    message(FATAL_ERROR "A minimum supported package rid is not specified (ex: win7-x86 or ubuntu.14.04-x64, osx.10.10-x64, rhel.7-x64)")
else()
    add_definitions(-DHOST_POLICY_PKG_NAME="runtime.${CLI_CMAKE_PKG_RID}.Microsoft.NETCore.DotNetHostPolicy")
    add_definitions(-DHOST_POLICY_PKG_REL_DIR="runtimes/${CLI_CMAKE_PKG_RID}/native")
endif()

if("${CLI_CMAKE_COMMIT_HASH}" STREQUAL "")
    message(FATAL_ERROR "Commit hash needs to be specified to build the host")
else()
    add_definitions(-DREPO_COMMIT_HASH="${CLI_CMAKE_COMMIT_HASH}")
endif()

