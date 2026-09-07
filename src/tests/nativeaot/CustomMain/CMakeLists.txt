# The nativeaot tests only build when TestBuildMode is nativeaot (see the DisabledProjects
# entry in src/tests/Common/dirs.proj), which never happens for browser wasm. The recursive
# add_subdirectory globber in src/tests/CMakeLists.txt reaches this directory anyway, so it
# has to exclude itself rather than compile native code nothing will ever link.
if (CLR_CMAKE_TARGET_ARCH_WASM)
    return()
endif()

project (CustomMainNative)
include_directories(${INC_PLATFORM_DIR})

add_library (CustomMainNative STATIC CustomMainNative.cpp)

# add the install targets
install (TARGETS CustomMainNative DESTINATION bin)
