include_directories( ${INC_PLATFORM_DIR} )
include_directories( "../../ServerContracts" )
include_directories( "../../NativeServer" )
include_directories("../")
set(SOURCES
    Client.cpp
    App.manifest)

# add the executable
add_executable (COMClientDispatch ${SOURCES})
target_link_libraries(COMClientDispatch ${LINK_LIBRARIES_ADDITIONAL})

# Copy CoreShim manifest to project output
file(GENERATE OUTPUT $<TARGET_FILE_DIR:COMClientDispatch>/CoreShim.X.manifest INPUT ${CMAKE_CURRENT_SOURCE_DIR}/CoreShim.X.manifest)

# add the install targets
install (TARGETS COMClientDispatch DESTINATION bin)
