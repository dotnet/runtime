add_definitions(-DSELF_NO_HOST)

list(APPEND UTILCODE_SOURCES ../hostimpl.cpp)

if(WIN32)
     add_definitions(-D_CRTIMP=) # use static version of crt

     add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_SOURCES)
endif(WIN32)

add_library(utilcodestaticnohost STATIC ${UTILCODE_SOURCES})
