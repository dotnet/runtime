cmake_minimum_required (VERSION 2.6)
project (MarshalArrayByValNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES MarshalArrayByValNative.cpp)

# add the executable
add_library (MarshalArrayByValNative SHARED ${SOURCES})

# add the install targets
install (TARGETS MarshalArrayByValNative DESTINATION bin)


