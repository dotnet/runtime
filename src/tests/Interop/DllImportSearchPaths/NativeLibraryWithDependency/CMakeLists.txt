project(NativeLibraryWithDependency)
include("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

add_library(Dependency SHARED Dependency.cpp)
target_link_libraries(Dependency PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

add_library(NativeLibraryWithDependency SHARED NativeLibraryWithDependency.cpp)
target_link_libraries(NativeLibraryWithDependency PRIVATE Dependency ${LINK_LIBRARIES_ADDITIONAL})
