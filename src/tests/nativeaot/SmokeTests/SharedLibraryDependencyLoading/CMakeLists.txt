project (SharedLibrary)
include_directories(${INC_PLATFORM_DIR})

add_executable(SharedLibraryHost SharedLibraryHost.cpp)
add_library(SharedLibraryDependency SHARED SharedLibraryDependency.cpp)

if (CLR_CMAKE_TARGET_UNIX)
    target_link_libraries (SharedLibraryHost PRIVATE ${CMAKE_DL_LIBS})
endif()

# If there's a dynamic ASAN runtime, then copy it to project output.
if (NOT "${ASAN_RUNTIME}" STREQUAL "")
    file(COPY "${ASAN_RUNTIME}" DESTINATION "${CMAKE_CURRENT_BINARY_DIR}")
endif()
