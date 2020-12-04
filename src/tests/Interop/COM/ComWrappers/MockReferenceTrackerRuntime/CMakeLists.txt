project (MockReferenceTrackerRuntime)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES ReferenceTrackerRuntime.cpp)

# add the shared library
add_library (MockReferenceTrackerRuntime SHARED ${SOURCES})
target_link_libraries(MockReferenceTrackerRuntime ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS MockReferenceTrackerRuntime DESTINATION bin)
