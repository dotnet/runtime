cmake_minimum_required(VERSION 3.10)
include(../../version.cmake)
project(eventheader-tracepoint-headers
    VERSION ${LINUXTRACEPOINTS_VERSION}
    DESCRIPTION "EventHeader-encoded Linux tracepoints for C/C++ (headers)"
    HOMEPAGE_URL "https://github.com/microsoft/LinuxTracepoints"
    LANGUAGES C CXX)
include(GNUInstallDirs)
include(CMakePackageConfigHelpers)

list(APPEND EVENTHEADER_HEADERS
    "${PROJECT_SOURCE_DIR}/eventheader/eventheader.h")

if(NOT WIN32)
    list(APPEND EVENTHEADER_HEADERS
    "${PROJECT_SOURCE_DIR}/eventheader/eventheader-tracepoint.h"
    "${PROJECT_SOURCE_DIR}/eventheader/EventHeaderDynamic.h"
    "${PROJECT_SOURCE_DIR}/eventheader/TraceLoggingProvider.h")
endif()

# eventheader-headers = EVENTHEADER_HEADERS
add_library(eventheader-headers INTERFACE)
target_include_directories(eventheader-headers
    INTERFACE
    "$<BUILD_INTERFACE:${PROJECT_SOURCE_DIR}/>"
    "$<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>")
set_target_properties(eventheader-headers PROPERTIES
    PUBLIC_HEADER "${EVENTHEADER_HEADERS}")
install(TARGETS eventheader-headers
    EXPORT eventheader-headersTargets
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/eventheader)
install(EXPORT eventheader-headersTargets
    FILE "eventheader-headersTargets.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/eventheader-headers")
configure_package_config_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/eventheader-headersConfig.cmake.in"
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-headersConfig.cmake"
    INSTALL_DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/eventheader-headers"
    NO_SET_AND_CHECK_MACRO
    NO_CHECK_REQUIRED_COMPONENTS_MACRO)
write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-headersConfigVersion.cmake"
    COMPATIBILITY SameMinorVersion)
install(FILES
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-headersConfig.cmake"
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-headersConfigVersion.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/eventheader-headers")
