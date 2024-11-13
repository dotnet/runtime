# tracepoint-decode = libtracepoint-decode, DECODE_HEADERS
add_library(tracepoint-decode
    PerfByteReader.cpp
    PerfDataFile.cpp
    PerfDataFileWriter.cpp
    PerfEventAbi.cpp
    PerfEventInfo.cpp
    PerfEventMetadata.cpp
    PerfEventSessionInfo.cpp)
target_include_directories(tracepoint-decode
    PUBLIC
    "$<BUILD_INTERFACE:${PROJECT_SOURCE_DIR}/include/>"
    "$<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>")
set(DECODE_HEADERS
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfByteReader.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfDataFile.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfDataFileDefs.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfDataFileWriter.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfEventAbi.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfEventInfo.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfEventMetadata.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/PerfEventSessionInfo.h")
set_target_properties(tracepoint-decode PROPERTIES
    PUBLIC_HEADER "${DECODE_HEADERS}")
target_compile_features(tracepoint-decode
    PRIVATE cxx_std_17)
install(TARGETS tracepoint-decode
    EXPORT tracepoint-decodeTargets
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/tracepoint)
install(EXPORT tracepoint-decodeTargets
    FILE "tracepoint-decodeTargets.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-decode")
configure_package_config_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/tracepoint-decodeConfig.cmake.in"
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-decodeConfig.cmake"
    INSTALL_DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-decode"
    NO_SET_AND_CHECK_MACRO
    NO_CHECK_REQUIRED_COMPONENTS_MACRO)
write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-decodeConfigVersion.cmake"
    COMPATIBILITY SameMinorVersion)
install(FILES
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-decodeConfig.cmake"
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-decodeConfigVersion.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-decode")
