project (Int128Native)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
  Int128Native.cpp
  UInt128Native.cpp
)
add_library (Int128Native ${TEST_LIB_TYPE} ${SOURCES})
install (TARGETS Int128Native DESTINATION bin)
