project (DisabledRuntimeMarshallingNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES DisabledRuntimeMarshallingNative.cpp )

# add the executable
add_library (DisabledRuntimeMarshallingNative SHARED ${SOURCES})
target_link_libraries(DisabledRuntimeMarshallingNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS DisabledRuntimeMarshallingNative DESTINATION bin)
