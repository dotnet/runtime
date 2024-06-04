project (COMClientPrimitives)
include_directories( ${INC_PLATFORM_DIR} )
include_directories( "../../ServerContracts" )
include_directories( "../../NativeServer" )
include_directories("../")
set(SOURCES
    Client.cpp
    NumericTests.cpp
    ArrayTests.cpp
    StringTests.cpp
    ErrorTests.cpp
    ColorTests.cpp
    App.manifest)

# add the executable
add_executable (COMClientPrimitives ${SOURCES})
target_link_libraries(COMClientPrimitives PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# Copy CoreShim manifest to project output
file(GENERATE OUTPUT $<TARGET_FILE_DIR:${PROJECT_NAME}>/CoreShim.X.manifest INPUT ${CMAKE_CURRENT_SOURCE_DIR}/CoreShim.X.manifest)

# add the install targets
install (TARGETS COMClientPrimitives DESTINATION bin)
# If there's a dynamic ASAN runtime, then copy it to project output.
if (NOT "${ASAN_RUNTIME}" STREQUAL "")
    file(COPY "${ASAN_RUNTIME}" DESTINATION "${CMAKE_CURRENT_BINARY_DIR}")
endif()
