include_directories(.)
if (CLR_CMAKE_HOST_UNIX)
    add_subdirectory(Unix)
else (CLR_CMAKE_HOST_UNIX)
    add_subdirectory(Windows)
endif (CLR_CMAKE_HOST_UNIX)

