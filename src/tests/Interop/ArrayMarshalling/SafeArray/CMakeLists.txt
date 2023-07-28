project (SafeArrayNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES SafeArrayNative.cpp RecordNative.cpp)

# add the executable
add_library (SafeArrayNative SHARED ${SOURCES})
target_link_libraries(SafeArrayNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS SafeArrayNative DESTINATION bin)
