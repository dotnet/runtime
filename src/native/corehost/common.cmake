# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(${DOTNET_PROJECT_NAME})

# Include directories
if(CLR_CMAKE_TARGET_WIN32)
    include_directories("${CLI_CMAKE_RESOURCE_DIR}/${DOTNET_PROJECT_NAME}")
endif()

set(RESOURCES)
if (CLR_CMAKE_TARGET_WIN32)
    if (NOT SKIP_VERSIONING)
        list(APPEND RESOURCES ${CMAKE_CURRENT_LIST_DIR}/native.rc)
    endif()
else()
    list(APPEND SOURCES ${VERSION_FILE_PATH})
endif()

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES ${HEADERS})
endif()

function(set_common_libs TargetType)

    # Libraries used for exe projects
    if (${TargetType} STREQUAL "exe")
        if((CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD) AND NOT CLR_CMAKE_TARGET_ANDROID)
            target_link_libraries (${DOTNET_PROJECT_NAME} PRIVATE "pthread")
        endif()
    endif()

    if (NOT ${TargetType} STREQUAL "lib-static")
        # Specify the import library to link against for Arm32 build since the default set is minimal
        if (CLR_CMAKE_TARGET_ARCH_ARM)
            if (CLR_CMAKE_TARGET_WIN32)
                target_link_libraries(${DOTNET_PROJECT_NAME} PRIVATE shell32.lib advapi32.lib)
            else()
                target_link_libraries(${DOTNET_PROJECT_NAME} PRIVATE atomic.a)
            endif()
        endif()

        target_link_libraries (${DOTNET_PROJECT_NAME} PRIVATE ${CMAKE_DL_LIBS})
    endif()
endfunction()
