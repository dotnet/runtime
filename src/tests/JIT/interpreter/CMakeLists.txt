project (pinvoke)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES pinvoke.cpp)

# add the executable
if (STATIC_LIBS_ONLY)
    add_library (pinvoke STATIC ${SOURCES})
else ()
    add_library (pinvoke SHARED ${SOURCES})
endif ()

# add the install targets
install (TARGETS pinvoke DESTINATION bin)
