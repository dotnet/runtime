project (COMClientLicensing)
include_directories( ${INC_PLATFORM_DIR} )
include_directories( "../../ServerContracts" )
include_directories( "../../NativeServer" )
set(SOURCES
    LicenseTests.cpp
    App.manifest)

# add the executable
add_executable (COMClientLicensing ${SOURCES})
target_link_libraries(COMClientLicensing ${LINK_LIBRARIES_ADDITIONAL})

# Copy CoreShim manifest to project output
file(GENERATE OUTPUT $<TARGET_FILE_DIR:${PROJECT_NAME}>/CoreShim.X.manifest INPUT ${CMAKE_CURRENT_SOURCE_DIR}/CoreShim.X.manifest)

# add the install targets
install (TARGETS COMClientLicensing DESTINATION bin)
