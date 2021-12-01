project (mscoree)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES ijwhost.cpp)

# add the shared library
add_library (ijwhost SHARED ${SOURCES})
target_link_libraries(ijwhost ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS ijwhost DESTINATION bin)
