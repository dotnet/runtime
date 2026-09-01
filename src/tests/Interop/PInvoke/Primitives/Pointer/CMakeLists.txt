include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    PointerNative.cpp
)
add_library (PointerNative ${TEST_LIB_TYPE} ${SOURCES})
install (TARGETS PointerNative DESTINATION bin)
