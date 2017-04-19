cmake_minimum_required (VERSION 2.6)
project (LPSTRTestNative)
set(SOURCES LPSTRTestNative.cpp )

# add the executable
add_library (LPSTRTestNative SHARED ${SOURCES})
target_link_libraries(LPSTRTestNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS LPSTRTestNative DESTINATION bin)
