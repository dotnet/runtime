project (IjwNativeVarargs)
include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwNativeVarargs.cpp)

# add the shared library
add_library (IjwNativeVarargs SHARED ${SOURCES})
target_link_libraries(IjwNativeVarargs ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS IjwNativeVarargs DESTINATION bin)
