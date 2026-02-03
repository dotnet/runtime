# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(VectorcallNative)
include("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

set(SOURCES VectorcallNative.cpp)

# add the shared library
add_library(${PROJECT_NAME} SHARED ${SOURCES})
target_link_libraries(${PROJECT_NAME} PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# On Windows, use a .def file to export functions without vectorcall name mangling
if(CLR_CMAKE_HOST_WIN32)
    set_target_properties(${PROJECT_NAME} PROPERTIES LINK_FLAGS "/DEF:${CMAKE_CURRENT_SOURCE_DIR}/VectorcallNative.def")
endif()

# add the install targets
install(TARGETS ${PROJECT_NAME} DESTINATION bin)
