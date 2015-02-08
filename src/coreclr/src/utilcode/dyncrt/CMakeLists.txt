
if(CLR_CMAKE_PLATFORM_UNIX)
    add_library(utilcode STATIC ${UTILCODE_SOURCES})
    add_dependencies(utilcode CoreClrPal)
else()
    add_library(utilcode STATIC ${UTILCODE_SOURCES})
endif(CLR_CMAKE_PLATFORM_UNIX)