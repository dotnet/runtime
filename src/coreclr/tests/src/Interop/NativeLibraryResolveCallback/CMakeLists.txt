cmake_minimum_required (VERSION 2.6)
project (ResolveLib)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES ResolveLib.cpp)

# add the executable
add_library (ResolveLib SHARED ${SOURCES})
target_link_libraries(ResolveLib ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS ResolveLib DESTINATION bin)


