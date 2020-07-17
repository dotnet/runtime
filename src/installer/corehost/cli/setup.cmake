# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

if (CLI_CMAKE_PORTABLE_BUILD)
    add_definitions(-DFEATURE_PORTABLE_BUILD=1)
endif ()

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

if("${CLI_CMAKE_COMMON_HOST_VER}" STREQUAL "")
    message(FATAL_ERROR "Common host version is not specified")
else()
    add_definitions(-DCOMMON_HOST_PKG_VER="${CLI_CMAKE_COMMON_HOST_VER}")
endif()

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

