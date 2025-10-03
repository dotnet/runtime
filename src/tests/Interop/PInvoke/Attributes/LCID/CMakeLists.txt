project (LCIDNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    LCIDNative.cpp
)
add_library (LCIDNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(LCIDNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
# add the install targets
install (TARGETS LCIDNative DESTINATION bin)
