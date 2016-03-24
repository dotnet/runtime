cmake_minimum_required (VERSION 2.6)
project (LPTSTRTestNative)
set(SOURCES LPTSTRTestNative.cpp )

# add the executable
add_library (LPTSTRTestNative SHARED ${SOURCES})

# add the install targets
install (TARGETS LPTSTRTestNative DESTINATION bin)