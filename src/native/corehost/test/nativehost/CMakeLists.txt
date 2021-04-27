# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(nativehost)

set(DOTNET_PROJECT_NAME "nativehost")

set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
set(MACOSX_RPATH ON)
if (CLR_CMAKE_TARGET_OSX)
    set(CMAKE_INSTALL_RPATH "@loader_path")
else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
endif()

include_directories(${CMAKE_CURRENT_LIST_DIR}/../../nethost)

set(SOURCES
    ./error_writer_redirector.cpp
    ./get_native_search_directories_test.cpp
    ./hostfxr_exports.cpp
    ./hostpolicy_exports.cpp
    ./host_context_test.cpp
    ./resolve_component_dependencies_test.cpp
    ./nativehost.cpp
)

set(HEADERS
    ./error_writer_redirector.h
    ./get_native_search_directories_test.h
    ./hostfxr_exports.h
    ./hostpolicy_exports.h
    ./host_context_test.h
    ./resolve_component_dependencies_test.h
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ./comhost_test.cpp)

    list(APPEND HEADERS
        ./comhost_test.h)

    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DELAYLOAD:nethost.dll")
endif()

include(../../hostmisc/hostmisc.cmake)

include(../testexe.cmake)

target_link_libraries(${DOTNET_PROJECT_NAME} nethost)

if (CLR_CMAKE_TARGET_WIN32)
    target_link_libraries(${DOTNET_PROJECT_NAME} delayimp.lib)
endif()

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    target_link_libraries(${DOTNET_PROJECT_NAME} Advapi32.lib Ole32.lib OleAut32.lib)
endif()
