cmake_minimum_required (VERSION 2.6)
project (UIntPtrNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES UIntPtrNative.cpp )

# add the executable
add_library (UIntPtrNative SHARED ${SOURCES})
target_link_libraries(UIntPtrNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS UIntPtrNative DESTINATION bin)