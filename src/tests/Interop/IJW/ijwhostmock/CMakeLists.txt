project (mscoree)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES mscoree.cpp)

# add the shared library
add_library (mscoree SHARED ${SOURCES})
target_link_libraries(mscoree ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS mscoree DESTINATION bin)
