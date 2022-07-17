project (IjwNativeCallingManagedDll)
include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwNativeCallingManagedDll.cpp)

# add the shared library
add_library (IjwNativeCallingManagedDll SHARED ${SOURCES})
target_link_libraries(IjwNativeCallingManagedDll ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS IjwNativeCallingManagedDll DESTINATION bin)
