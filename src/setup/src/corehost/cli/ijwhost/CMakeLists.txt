# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(ijwhost)

set(DOTNET_PROJECT_NAME "ijwhost")

# Include directories
include_directories(../fxr)
include_directories(${ARCH_SPECIFIC_FOLDER_NAME})

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ijwthunk.cpp
    ijwhost.cpp
    ../fxr_resolver.cpp
    ../../common/trace.cpp
    ../../common/utils.cpp
    ../fxr/fx_ver.cpp
    pedecoder.cpp
    bootstrap_thunk_chunk.cpp
    ${ARCH_SPECIFIC_FOLDER_NAME}/bootstrap_thunk.cpp
)

if(WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

set (ASM_HELPERS_SOURCES
    ${ARCH_SPECIFIC_FOLDER_NAME}/asmhelpers.asm)

add_definitions(-DFEATURE_LIBHOST=1)

convert_to_absolute_path(SOURCES ${SOURCES})
convert_to_absolute_path(ASM_HELPERS_SOURCES ${ASM_HELPERS_SOURCES})

if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))

    get_include_directories_asm(ASM_INCLUDE_DIRECTORIES)

    foreach(ASM_FILE ${ASM_HELPERS_SOURCES})
        # Inserts a custom command in CMake build to preprocess each asm source file
        get_filename_component(name ${ASM_FILE} NAME_WE)
        file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/${name}.asm" ASM_PREPROCESSED_FILE)
        preprocess_def_file(${ASM_FILE} ${ASM_PREPROCESSED_FILE})

        # We do not pass any defines since we have already done pre-processing above
        set (ASM_CMDLINE "-o ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj ${ASM_PREPROCESSED_FILE}")

        # Generate the batch file that will invoke the assembler
        file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/runasm_${name}.cmd" ASM_SCRIPT_FILE)

        file(GENERATE OUTPUT "${ASM_SCRIPT_FILE}"
                CONTENT "\"${CMAKE_ASM_MASM_COMPILER}\" -g ${ASM_INCLUDE_DIRECTORIES} ${ASM_CMDLINE}")

        message("Generated  - ${ASM_SCRIPT_FILE}")

        # Need to compile asm file using custom command as include directories are not provided to asm compiler
        add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj
                            COMMAND ${ASM_SCRIPT_FILE}
                            DEPENDS ${ASM_PREPROCESSED_FILE}
                            COMMENT "Assembling ${ASM_PREPROCESSED_FILE} - ${ASM_SCRIPT_FILE}")

        # mark obj as source that does not require compile
        set_source_files_properties(${CMAKE_CURRENT_BINARY_DIR}/${name}.obj PROPERTIES EXTERNAL_OBJECT TRUE)

        # Add the generated OBJ in the dependency list so that it gets consumed during linkage
        set(ASM_HELPERS_SOURCES ${ASM_HELPERS_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj)
    endforeach()
endif ()

if (WIN32 AND CLI_CMAKE_PLATFORM_ARCH_I386)
    set_source_files_properties(${ASM_HELPERS_SOURCES} PROPERTIES COMPILE_FLAGS "/safeseh")
endif ()

list(APPEND SOURCES ${ASM_HELPERS_SOURCES})

include(../lib.cmake)

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))
    target_link_libraries(ijwhost Advapi32.lib Ole32.lib)
endif()

install(TARGETS ijwhost DESTINATION corehost)
install_symbols (ijwhost corehost) 