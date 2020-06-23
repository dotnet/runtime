project (COMClientEvents)
include_directories( ${INC_PLATFORM_DIR} )
include_directories( "../../ServerContracts" )
include_directories( "../../NativeServer" )
set(SOURCES
    EventTests.cpp
    App.manifest)

# add the executable
add_executable (COMClientEvents ${SOURCES})
target_link_libraries(COMClientEvents ${LINK_LIBRARIES_ADDITIONAL})

# Copy CoreShim manifest to project output
file(GENERATE OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/CoreShim.X.manifest INPUT ${CMAKE_CURRENT_SOURCE_DIR}/CoreShim.X.manifest)

# add the install targets
install (TARGETS COMClientEvents DESTINATION bin)
