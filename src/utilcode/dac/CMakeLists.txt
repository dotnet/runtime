
include(${CLR_DIR}/dac.cmake)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_library(utilcode_dac STATIC ${UTILCODE_SOURCES})
    add_dependencies(utilcode_dac CoreClrPal)
else()
    add_definitions(-DSELF_NO_HOST)
    list(APPEND UTILCODE_SOURCES ../hostimpl.cpp)
    add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_SOURCES)
    add_library(utilcode_dac STATIC ${UTILCODE_SOURCES})
endif(CLR_CMAKE_PLATFORM_UNIX)