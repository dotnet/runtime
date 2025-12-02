project (CustomMainWithStubExeNative)
include_directories(${INC_PLATFORM_DIR})

add_executable (CustomMainWithStubExeNative CustomMainWithStubExeNative.cpp)

if (CLR_CMAKE_TARGET_UNIX)
    target_link_libraries (CustomMainWithStubExeNative PRIVATE ${CMAKE_DL_LIBS})
endif()

# add the install targets
install (TARGETS CustomMainWithStubExeNative DESTINATION bin)

# If there's a dynamic ASAN runtime, then copy it to project output.
if (NOT "${ASAN_RUNTIME}" STREQUAL "")
    file(COPY "${ASAN_RUNTIME}" DESTINATION "${CMAKE_CURRENT_BINARY_DIR}")
endif()