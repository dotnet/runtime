project (NativeLibWithReversePInvokes)
include_directories(${INC_PLATFORM_DIR})

if (CLR_CMAKE_TARGET_OSX)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        "-undefined dynamic_lookup"
    )
else (CLR_CMAKE_TARGET_WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        "/FORCE:UNRESOLVED"
    )
endif ()

add_library (NativeLibWithReversePInvokes SHARED NativeLibWithReversePInvokes.cpp)
target_link_libraries(NativeLibWithReversePInvokes PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS NativeLibWithReversePInvokes DESTINATION bin)
