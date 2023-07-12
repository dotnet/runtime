project (IUnknownNativeALC)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES IUnknownNativeALC.cpp)

# add the executable
add_library (IUnknownNativeALC SHARED ${SOURCES})
target_link_libraries(IUnknownNativeALC PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS IUnknownNativeALC DESTINATION bin)


