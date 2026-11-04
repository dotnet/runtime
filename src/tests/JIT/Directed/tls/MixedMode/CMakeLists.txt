# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_HOST_WIN32)
    add_subdirectory(cppcli)
    add_subdirectory(cppcli-mutual)
endif()
