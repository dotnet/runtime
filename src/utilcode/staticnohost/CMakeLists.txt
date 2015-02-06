add_definitions(-DSELF_NO_HOST)

if(WIN32)
     add_definitions(-D_CRTIMP=) # use static version of crt

     set(UTILCODE_SOURCES ${UTILCODE_SOURCES} ../hostimpl.cpp)

     add_library(utilcodestaticnohost STATIC ${UTILCODE_SOURCES})
elseif(CLR_CMAKE_PLATFORM_UNIX)
     add_library(utilcodestaticnohost STATIC ${UTILCODE_SOURCES})
endif(WIN32)