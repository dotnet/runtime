add_definitions(-DSELF_NO_HOST)

if(WIN32)
     add_definitions(-D_CRTIMP=) # use static version of crt

     list(APPEND UTILCODE_SOURCES ../hostimpl.cpp)
     add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_SOURCES)
     add_library(utilcodestaticnohost STATIC ${UTILCODE_SOURCES})
elseif(CLR_CMAKE_PLATFORM_UNIX)
     add_library(utilcodestaticnohost STATIC ${UTILCODE_SOURCES})
endif(WIN32)
