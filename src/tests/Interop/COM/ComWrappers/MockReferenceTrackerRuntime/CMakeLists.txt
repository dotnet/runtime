project (MockReferenceTrackerRuntime)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES ReferenceTrackerRuntime.cpp)

# add the shared library
add_library (MockReferenceTrackerRuntime ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(MockReferenceTrackerRuntime PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS MockReferenceTrackerRuntime DESTINATION bin)
