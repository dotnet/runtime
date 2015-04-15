
if(CLR_CMAKE_PLATFORM_UNIX)
    add_library(utilcode STATIC ${UTILCODE_SOURCES})
    add_dependencies(utilcode coreclrpal)
else()
    add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_SOURCES)
    add_library(utilcode STATIC ${UTILCODE_SOURCES})
endif(CLR_CMAKE_PLATFORM_UNIX)