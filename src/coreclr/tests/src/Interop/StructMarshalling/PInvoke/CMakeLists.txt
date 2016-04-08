cmake_minimum_required (VERSION 2.6)
project (MarshalStructAsParam)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES MarshalStructAsParamDLL.cpp)

# add the executable
add_library (MarshalStructAsParam SHARED ${SOURCES})

# add the install targets
install (TARGETS MarshalStructAsParam DESTINATION bin)