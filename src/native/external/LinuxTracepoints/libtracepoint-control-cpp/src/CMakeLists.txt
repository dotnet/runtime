# tracepoint-control = libtracepoint-control, CONTROL_HEADERS
add_library(tracepoint-control
    "TracepointCache.cpp"
    "TracepointPath.cpp"
    "TracepointSession.cpp"
    "TracepointSpec.cpp"
    "UniqueHandles.cpp")
target_include_directories(tracepoint-control
    PUBLIC
    "$<BUILD_INTERFACE:${PROJECT_SOURCE_DIR}/include/>"
    "$<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>")
target_link_libraries(tracepoint-control
    PUBLIC tracepoint-decode atomic)
set(CONTROL_HEADERS
    "${PROJECT_SOURCE_DIR}/include/tracepoint/TracepointCache.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/TracepointName.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/TracepointPath.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/TracepointSession.h"
    "${PROJECT_SOURCE_DIR}/include/tracepoint/TracepointSpec.h")
set_target_properties(tracepoint-control PROPERTIES
    PUBLIC_HEADER "${CONTROL_HEADERS}")
target_compile_features(tracepoint-control
    PRIVATE cxx_std_17)
install(TARGETS tracepoint-control
    EXPORT tracepoint-controlTargets
    PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/tracepoint)
install(EXPORT tracepoint-controlTargets
    FILE "tracepoint-controlTargets.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-control")
configure_package_config_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/tracepoint-controlConfig.cmake.in"
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-controlConfig.cmake"
    INSTALL_DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-control"
    NO_SET_AND_CHECK_MACRO
    NO_CHECK_REQUIRED_COMPONENTS_MACRO)
write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-controlConfigVersion.cmake"
    COMPATIBILITY SameMinorVersion)
install(FILES
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-controlConfig.cmake"
    "${CMAKE_CURRENT_BINARY_DIR}/tracepoint-controlConfigVersion.cmake"
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/tracepoint-control")
