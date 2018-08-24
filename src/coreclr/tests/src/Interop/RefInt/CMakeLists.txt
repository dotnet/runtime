cmake_minimum_required (VERSION 2.6)
project (RefIntNative)
set(SOURCES RefIntNative.cpp )

# add the executable
add_library(RefIntNative SHARED ${SOURCES})
target_link_libraries(RefIntNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS RefIntNative DESTINATION bin)