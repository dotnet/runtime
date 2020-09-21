project (UnmanagedCallersOnlyDll)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES UnmanagedCallersOnlyDll.cpp )

# add the executable
add_library (UnmanagedCallersOnlyDll SHARED ${SOURCES})
target_link_libraries(UnmanagedCallersOnlyDll ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS UnmanagedCallersOnlyDll DESTINATION bin)
