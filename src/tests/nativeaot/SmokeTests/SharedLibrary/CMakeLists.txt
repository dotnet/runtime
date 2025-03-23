project (SharedLibrary)
include_directories(${INC_PLATFORM_DIR})

if (CLR_CMAKE_TARGET_WIN32)
    # Regression test for https://github.com/dotnet/runtime/issues/105330, enable CFG on the EXE
    SET(CMAKE_CXX_FLAGS  "${CMAKE_CXX_FLAGS} /guard:cf")
    SET(CMAKE_EXE_LINKER_FLAGS  "${CMAKE_EXE_LINKER_FLAGS} /guard:cf")
endif()

add_executable (SharedLibraryDriver SharedLibrary.cpp)

if (CLR_CMAKE_TARGET_UNIX)
    target_link_libraries (SharedLibraryDriver PRIVATE ${CMAKE_DL_LIBS})
endif()

# add the install targets
install (TARGETS SharedLibraryDriver DESTINATION bin)
# If there's a dynamic ASAN runtime, then copy it to project output.
if (NOT "${ASAN_RUNTIME}" STREQUAL "")
    file(COPY "${ASAN_RUNTIME}" DESTINATION "${CMAKE_CURRENT_BINARY_DIR}")
endif()
