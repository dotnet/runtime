
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    DecimalTestNative.cpp
)
add_library (DecimalTestNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(DecimalTestNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
install (TARGETS DecimalTestNative DESTINATION bin)
