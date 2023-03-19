project (NativeLibWithReversePInvokes)
include_directories(${INC_PLATFORM_DIR})

add_library (NativeLibWithReversePInvokes SHARED NativeLibWithReversePInvokes.cpp)
target_link_libraries(NativeLibWithReversePInvokes PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS NativeLibWithReversePInvokes DESTINATION bin)
