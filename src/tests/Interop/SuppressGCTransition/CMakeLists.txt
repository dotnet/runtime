project (SuppressGCTransitionNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    SuppressGCTransitionNative.cpp
)
add_library (SuppressGCTransitionNative SHARED ${SOURCES})
target_link_libraries(SuppressGCTransitionNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
# add the install targets
install (TARGETS SuppressGCTransitionNative DESTINATION bin)
