add_definitions(-MT)
add_executable(GenClrDebugResource GenClrDebugResource.cpp)

target_link_libraries(GenClrDebugResource
    ${STATIC_MT_CRT_LIB}
)

# ARM64_TODO: Enable this for Windows Arm64
if (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
	target_link_libraries(GenClrDebugResource ${STATIC_MT_VCRT_LIB})
endif()