add_definitions(-DSELF_NO_HOST)
add_definitions(-D_CRTIMP=) # use static version of crt

set(UTILCODE_SOURCES ${UTILCODE_SOURCES} ../hostimpl.cpp)

add_library(utilcodestaticnohost STATIC ${UTILCODE_SOURCES})
