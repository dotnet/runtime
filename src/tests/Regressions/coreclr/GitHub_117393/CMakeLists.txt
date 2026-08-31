project (nativetest117393)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES nativetest117393.cpp)

# add the shared library
add_library (nativetest117393 SHARED ${SOURCES})
target_link_libraries(nativetest117393 PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS nativetest117393 DESTINATION bin)
