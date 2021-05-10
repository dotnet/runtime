# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(ijwhost)

set(DOTNET_PROJECT_NAME "ijwhost")

# Include directories
include_directories(../fxr)
include_directories(${ARCH_SOURCES_DIR})

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ijwthunk.cpp
    ijwhost.cpp
    ../fxr_resolver.cpp
    pedecoder.cpp
    bootstrap_thunk_chunk.cpp
    ${ARCH_SOURCES_DIR}/bootstrap_thunk.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

set (ASM_HELPERS_SOURCES
    ${ARCH_SOURCES_DIR}/asmhelpers.asm)

add_definitions(-DFEATURE_LIBHOST=1)

convert_to_absolute_path(SOURCES ${SOURCES})
convert_to_absolute_path(ASM_HELPERS_SOURCES ${ASM_HELPERS_SOURCES})

if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    preprocess_files(ASM_HELPERS_SOURCES ${ASM_HELPERS_SOURCES})
    if (CMAKE_GENERATOR MATCHES "Visual Studio")
        compile_asm(TARGET ijwhost ASM_FILES ${ASM_HELPERS_SOURCES} OUTPUT_OBJECTS ASM_HELPERS_SOURCES)
    endif()
endif ()

if (CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_I386)
    set_source_files_properties(${ASM_HELPERS_SOURCES} PROPERTIES COMPILE_FLAGS "/safeseh")
endif ()

list(APPEND SOURCES ${ASM_HELPERS_SOURCES})

include(../lib.cmake)

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    target_link_libraries(ijwhost Advapi32.lib Ole32.lib)
endif()

install_with_stripped_symbols(ijwhost TARGETS corehost)
