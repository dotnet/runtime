project (IndirectNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES IndirectNative.cpp)
add_library (IndirectNative SHARED ${SOURCES})
# add the install targets
install (TARGETS IndirectNative DESTINATION bin)
