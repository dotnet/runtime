project (Decimal128Native)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
  Decimal128Native.cpp
)
add_library (Decimal128Native ${TEST_LIB_TYPE} ${SOURCES})
install (TARGETS Decimal128Native DESTINATION bin)
