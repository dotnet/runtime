project (WeakReferenceNative)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES WeakReferenceNative.cpp)

# add the shared library
add_library (WeakReferenceNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(WeakReferenceNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS WeakReferenceNative DESTINATION bin)
