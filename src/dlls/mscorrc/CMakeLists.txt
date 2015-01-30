include_directories("../../pal/prebuilt/corerror")
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
add_subdirectory(full)
add_subdirectory(small)