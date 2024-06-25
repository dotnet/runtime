cmake_minimum_required(VERSION 3.10)
include(../../version.cmake)
project(tracepoint-headers
    VERSION ${LINUXTRACEPOINTS_VERSION}
    DESCRIPTION "Linux tracepoints interface for C/C++ (headers)"
    HOMEPAGE_URL "https://github.com/microsoft/LinuxTracepoints"
    LANGUAGES C CXX)
include(GNUInstallDirs)
include(CMakePackageConfigHelpers)

list(APPEND TRACEPOINT_HEADERS
    "${PROJECT_SOURCE_DIR}/tracepoint/tracepoint.h")

if(NOT WIN32)
    list(APPEND TRACEPOINT_HEADERS
    "${PROJECT_SOURCE_DIR}/tracepoint/tracepoint-impl.h"
    "${PROJECT_SOURCE_DIR}/tracepoint/tracepoint-provider.h"
    "${PROJECT_SOURCE_DIR}/tracepoint/tracepoint-state.h")
endif()

# tracepoint-headers = TRACEPOINT_HEADERS
add_library(tracepoint-headers INTERFACE)
target_include_directories(tracepoint-headers
    INTERFACE
    "$<BUILD_INTERFACE:${PROJECT_SOURCE_DIR}/>"
    "$<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>")
set_target_properties(tracepoint-headers PROPERTIES
    PUBLIC_HEADER "${TRACEPOINT_HEADERS}")
install(TARGETS tracepoint-headers
    EXPORT tracepoint-headersTargets
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/tracepoint)
install(EXPORT tracepoint-headersTargets
    FILE "tracepoint-headersTargets.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-headers")
configure_package_config_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/tracepoint-headersConfig.cmake.in"
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-headersConfig.cmake"
    INSTALL_DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-headers"
    NO_SET_AND_CHECK_MACRO
    NO_CHECK_REQUIRED_COMPONENTS_MACRO)
write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-headersConfigVersion.cmake"
    COMPATIBILITY SameMinorVersion)
install(FILES
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-headersConfig.cmake"
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-headersConfigVersion.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-headers")
