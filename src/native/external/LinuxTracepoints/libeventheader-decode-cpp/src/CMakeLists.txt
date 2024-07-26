# eventheader-decode = libeventheader-decode, DECODE_HEADERS
add_library(eventheader-decode
    EventEnumerator.cpp
    EventFormatter.cpp)
target_include_directories(eventheader-decode
    PUBLIC
    "$<BUILD_INTERFACE:${PROJECT_SOURCE_DIR}/include/>"
    "$<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>")
target_link_libraries(eventheader-decode
    PUBLIC eventheader-headers
    PRIVATE tracepoint-decode)
set(DECODE_HEADERS
    "${PROJECT_SOURCE_DIR}/include/eventheader/EventEnumerator.h"
    "${PROJECT_SOURCE_DIR}/include/eventheader/EventFormatter.h")
set_target_properties(eventheader-decode PROPERTIES
    PUBLIC_HEADER "${DECODE_HEADERS}")
target_compile_features(eventheader-decode
    PRIVATE cxx_std_17)
install(TARGETS eventheader-decode
    EXPORT eventheader-decodeTargets
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/eventheader)
install(EXPORT eventheader-decodeTargets
    FILE "eventheader-decodeTargets.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/eventheader-decode")
configure_package_config_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/eventheader-decodeConfig.cmake.in"
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-decodeConfig.cmake"
    INSTALL_DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/eventheader-decode"
    NO_SET_AND_CHECK_MACRO
    NO_CHECK_REQUIRED_COMPONENTS_MACRO)
write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-decodeConfigVersion.cmake"
    COMPATIBILITY SameMinorVersion)
install(FILES
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-decodeConfig.cmake"
    "${CMAKE_CURRENT_BINARY_DIR}/eventheader-decodeConfigVersion.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/eventheader-decode")
