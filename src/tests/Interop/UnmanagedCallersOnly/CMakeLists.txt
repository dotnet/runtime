project (UnmanagedCallersOnlyDll)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES UnmanagedCallersOnlyDll.cpp )

# add the executable
add_library (UnmanagedCallersOnlyDll ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(UnmanagedCallersOnlyDll PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS UnmanagedCallersOnlyDll DESTINATION bin)
