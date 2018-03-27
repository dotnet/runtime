cmake_minimum_required (VERSION 2.6)
project (ClassicCOMNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES ClassicCOMNative.cpp)

# add the executable
add_library (ClassicCOMNative SHARED ${SOURCES})
target_link_libraries(ClassicCOMNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS ClassicCOMNative DESTINATION bin)


