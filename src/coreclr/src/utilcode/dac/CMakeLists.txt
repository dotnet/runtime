
include(${CLR_DIR}/dac.cmake)

add_definitions(-DSELF_NO_HOST)
list(APPEND UTILCODE_SOURCES ../hostimpl.cpp)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_library(utilcode_dac STATIC ${UTILCODE_SOURCES})
    target_link_libraries(utilcode_dac nativeresourcestring)
    add_dependencies(utilcode_dac coreclrpal)
else()
    add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_SOURCES)
    add_library(utilcode_dac STATIC ${UTILCODE_SOURCES})
endif(CLR_CMAKE_PLATFORM_UNIX)
