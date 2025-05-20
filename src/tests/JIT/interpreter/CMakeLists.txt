project (pinvoke)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES pinvoke.cpp)

# add the executable
add_library (pinvoke SHARED ${SOURCES})

# add the install targets
install (TARGETS pinvoke DESTINATION bin)
