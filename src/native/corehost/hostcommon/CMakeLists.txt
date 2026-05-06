# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include(files.cmake)

add_library(libhostcommon STATIC ${SOURCES})

target_compile_definitions(libhostcommon PRIVATE EXPORT_SHARED_API)
set_target_properties(libhostcommon PROPERTIES MACOSX_RPATH TRUE)

if (NOT CLR_CMAKE_TARGET_WIN32)
    target_sources(libhostcommon PRIVATE ${VERSION_FILE_PATH})
endif()

if (MSVC)
    target_sources(libhostcommon PRIVATE ${HEADERS})
endif()

target_link_libraries(libhostcommon PUBLIC hostmisc)
