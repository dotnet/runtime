# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

if(CLR_SINGLE_FILE_HOST_ONLY)
    # CLR partition builds only the single file host where hosting components are all statically linked.
    # the versioning information is irrelevant and may only come up in tracing.
    # so we will use "static"
    add_definitions(-DHOST_POLICY_PKG_NAME="static")
    add_definitions(-DHOST_POLICY_PKG_REL_DIR="static")
    add_definitions(-DREPO_COMMIT_HASH="static")
else()
    if("${CLI_CMAKE_PKG_RID}" STREQUAL "")
        message(FATAL_ERROR "A minimum supported package rid is not specified (ex: win7-x86 or ubuntu.14.04-x64, osx.10.12-x64, rhel.7-x64)")
    else()
        add_definitions(-DHOST_POLICY_PKG_NAME="runtime.${CLI_CMAKE_PKG_RID}.Microsoft.NETCore.DotNetHostPolicy")
        add_definitions(-DHOST_POLICY_PKG_REL_DIR="runtimes/${CLI_CMAKE_PKG_RID}/native")
    endif()

    if("${CLI_CMAKE_COMMIT_HASH}" STREQUAL "")
        message(FATAL_ERROR "Commit hash needs to be specified to build the host")
    else()
        add_definitions(-DREPO_COMMIT_HASH="${CLI_CMAKE_COMMIT_HASH}")
    endif()
endif()

if("${CLI_CMAKE_FALLBACK_OS}" STREQUAL "")
    message(FATAL_ERROR "Fallback rid needs to be specified to build the host")
else()
    add_definitions(-DFALLBACK_HOST_OS="${CLI_CMAKE_FALLBACK_OS}")
endif()

add_definitions(-DCURRENT_OS_NAME="${CLR_CMAKE_TARGET_OS}")
add_definitions(-DCURRENT_ARCH_NAME="${CLR_CMAKE_TARGET_ARCH}")
if("${CLI_CMAKE_FALLBACK_OS}" STREQUAL "${CLR_CMAKE_TARGET_OS}")
    add_definitions(-DFALLBACK_OS_IS_SAME_AS_TARGET_OS)
endif()
