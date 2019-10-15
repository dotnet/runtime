cmake_minimum_required (VERSION 2.6)
project (ResolvedLib)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES ResolvedLib.cpp)

# add the executable
add_library (ResolvedLib SHARED ${SOURCES})
target_link_libraries(ResolvedLib ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS ResolvedLib DESTINATION bin)


