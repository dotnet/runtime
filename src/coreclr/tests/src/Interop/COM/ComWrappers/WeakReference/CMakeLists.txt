project (WeakReferenceNative)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES WeakReferenceNative.cpp)

# add the shared library
add_library (WeakReferenceNative SHARED ${SOURCES})
target_link_libraries(WeakReferenceNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS WeakReferenceNative DESTINATION bin)
