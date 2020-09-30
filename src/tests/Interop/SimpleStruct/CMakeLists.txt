project (SimpleStructNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES SimpleStructNative.cpp )

# add the executable
add_library (SimpleStructNative SHARED ${SOURCES})
target_link_libraries(SimpleStructNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS SimpleStructNative DESTINATION bin)