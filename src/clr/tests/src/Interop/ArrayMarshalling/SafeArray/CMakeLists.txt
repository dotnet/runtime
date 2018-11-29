cmake_minimum_required (VERSION 2.6)
project (SafeArrayNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES SafeArrayNative.cpp RecordNative.cpp)

# add the executable
add_library (SafeArrayNative SHARED ${SOURCES})
target_link_libraries(SafeArrayNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS SafeArrayNative DESTINATION bin)
