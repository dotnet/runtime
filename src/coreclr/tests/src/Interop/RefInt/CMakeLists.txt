project (RefIntNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES RefIntNative.cpp )

# add the executable
add_library(RefIntNative SHARED ${SOURCES})
target_link_libraries(RefIntNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS RefIntNative DESTINATION bin)
