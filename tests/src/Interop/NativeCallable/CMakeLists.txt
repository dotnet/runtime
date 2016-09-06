cmake_minimum_required (VERSION 2.6)
project (NativeCallableDll)
set(SOURCES NativeCallableDll.cpp )

# add the executable
add_library (NativeCallableDll SHARED ${SOURCES})
target_link_libraries(NativeCallableDll ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS NativeCallableDll DESTINATION bin)
