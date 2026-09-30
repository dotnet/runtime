cmake_minimum_required(VERSION 3.26)
# This is an app-packaging input, not a native test-discovery project.
if(NOT CMAKE_SOURCE_DIR STREQUAL CMAKE_CURRENT_SOURCE_DIR)
    return()
endif()

project(InProcCrashReportAppleNative)

include(../../../../../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

if(NOT CLR_CMAKE_TARGET_APPLE_MOBILE OR NOT CLR_CMAKE_TARGET_ARCH_ARM64)
    message(FATAL_ERROR "This adapter requires an Apple mobile arm64 target.")
endif()

if(NOT EXISTS "${CRASH_REPORT_MINIPAL_LIBRARY}")
    message(FATAL_ERROR "Build the matching CoreCLR first; missing minipal: ${CRASH_REPORT_MINIPAL_LIBRARY}")
endif()

include_directories(${CLR_SRC_NATIVE_DIR})
set(LINK_LIBRARIES_ADDITIONAL "${CRASH_REPORT_MINIPAL_LIBRARY}")
set(CRASH_REPORT_APPLE_APP TRUE)
add_subdirectory(
    "${CLR_REPO_ROOT_DIR}/src/tests/baseservices/exceptions/inproccrashreport/Shared"
    fixture)
