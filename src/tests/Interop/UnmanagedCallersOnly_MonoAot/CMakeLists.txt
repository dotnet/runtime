project (UnmanagedCallersOnly_MonoAotDll)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES UnmanagedCallersOnly_MonoAotDll.cpp )

# add the executable
add_library (UnmanagedCallersOnly_MonoAotDll SHARED ${SOURCES})
target_link_libraries(UnmanagedCallersOnly_MonoAotDll ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS UnmanagedCallersOnly_MonoAotDll DESTINATION bin)
