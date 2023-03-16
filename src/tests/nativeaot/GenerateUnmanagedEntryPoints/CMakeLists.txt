project (GenerateUnmanagedEntryPoints)
include_directories(${INC_PLATFORM_DIR})

add_executable (GenerateUnmanagedEntryPointsDriver GenerateUnmanagedEntryPoints.cpp)

if (CLR_CMAKE_TARGET_UNIX)
    target_link_libraries (GenerateUnmanagedEntryPointsDriver PRIVATE ${CMAKE_DL_LIBS})
endif()
