# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

include(../settings.cmake)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_subdirectory(Host)
add_subdirectory(CopyWin32Resources)
