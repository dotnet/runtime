project (COMNativeServer)
include_directories( ${INC_PLATFORM_DIR} )
include_directories( "../ServerContracts" )
set(SOURCES Servers.cpp stdafx.cpp Exports.def COMNativeServer.X.manifest)

if (CLR_CMAKE_HOST_WIN32)
    # 4365 - signed/unsigned mismatch
    add_compile_options(-wd4365)
endif()

# add the shared library
add_library (COMNativeServer SHARED ${SOURCES})
target_link_libraries(COMNativeServer ${LINK_LIBRARIES_ADDITIONAL})

# Copy manifest file to project output
file(GENERATE OUTPUT $<TARGET_FILE_DIR:${PROJECT_NAME}>/COMNativeServer.X.manifest INPUT ${CMAKE_CURRENT_SOURCE_DIR}/COMNativeServer.X.manifest)

# add the install targets
install (TARGETS COMNativeServer DESTINATION bin)
