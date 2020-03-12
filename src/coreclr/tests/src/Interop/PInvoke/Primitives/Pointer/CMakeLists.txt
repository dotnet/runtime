include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    NonBlittablePointerNative.cpp
)
add_library (NonBlittablePointerNative SHARED ${SOURCES})
install (TARGETS NonBlittablePointerNative DESTINATION bin)
