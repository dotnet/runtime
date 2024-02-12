project (FunctionPointerNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES FunctionPointerNative.cpp FunctionPointerNative.def)

# add the executable
add_library (FunctionPointerNative SHARED ${SOURCES})
target_link_libraries(FunctionPointerNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS FunctionPointerNative DESTINATION bin)


