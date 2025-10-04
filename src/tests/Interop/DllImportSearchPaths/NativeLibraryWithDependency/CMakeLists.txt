project(NativeLibraryWithDependency)
include("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# Remove setting dependent load flags to LOAD_LIBRARY_SEARCH_SYSTEM32 - this test is explicitly around a local dependency
if (CLR_CMAKE_TARGET_WIN32)
    string(REPLACE "/DEPENDENTLOADFLAG:0x800" "" CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS}")
endif ()

add_library(Dependency ${TEST_LIB_TYPE} Dependency.cpp)
target_link_libraries(Dependency PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

add_library(NativeLibraryWithDependency ${TEST_LIB_TYPE} NativeLibraryWithDependency.cpp)
target_link_libraries(NativeLibraryWithDependency PRIVATE Dependency ${LINK_LIBRARIES_ADDITIONAL})
