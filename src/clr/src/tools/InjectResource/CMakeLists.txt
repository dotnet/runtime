remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)
add_definitions(-MT)

add_executable(InjectResource InjectResource.cpp)

target_link_libraries(InjectResource
    ${STATIC_MT_CRT_LIB}
)

# ARM64_TODO: Enable this for Windows Arm64
if (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
    target_link_libraries(InjectResource ${STATIC_MT_VCRT_LIB})
endif()