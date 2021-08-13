project(SetLastErrorNative)
include("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES SetLastErrorNative.cpp)

# add the shared library
add_library(SetLastErrorNative SHARED ${SOURCES})
target_link_libraries(SetLastErrorNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install(TARGETS SetLastErrorNative DESTINATION bin)
