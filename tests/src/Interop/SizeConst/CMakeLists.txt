cmake_minimum_required (VERSION 2.6)
project (SizeConstNative)
set(SOURCES SizeConstNative.cpp)

# add the executable
add_library (SizeConstNative SHARED ${SOURCES})

# add the install targets
install (TARGETS SizeConstNative DESTINATION bin)


