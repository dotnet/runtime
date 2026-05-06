project (LCIDNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    LCIDNative.cpp
)
add_library (LCIDNative SHARED ${SOURCES})
target_link_libraries(LCIDNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
# add the install targets
install (TARGETS LCIDNative DESTINATION bin)
