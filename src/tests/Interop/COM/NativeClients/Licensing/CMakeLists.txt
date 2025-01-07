project (COMClientLicensing)
include_directories( ${INC_PLATFORM_DIR} )
include_directories( "../../ServerContracts" )
include_directories( "../../NativeServer" )
include_directories("../")
set(SOURCES
    LicenseTests.cpp
    App.manifest)

# add the executable
add_executable (COMClientLicensing ${SOURCES})
target_link_libraries(COMClientLicensing PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# Copy CoreShim manifest to project output
file(GENERATE OUTPUT $<TARGET_FILE_DIR:${PROJECT_NAME}>/CoreShim.X.manifest INPUT ${CMAKE_CURRENT_SOURCE_DIR}/CoreShim.X.manifest)

# add the install targets
install (TARGETS COMClientLicensing DESTINATION bin)
# If there's a dynamic ASAN runtime, then copy it to project output.
if (NOT "${ASAN_RUNTIME}" STREQUAL "")
    file(COPY "${ASAN_RUNTIME}" DESTINATION "${CMAKE_CURRENT_BINARY_DIR}")
endif()
