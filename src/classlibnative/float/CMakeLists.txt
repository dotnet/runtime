set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories("../inc")

set(FLOAT_SOURCES
    floatdouble.cpp
    floatsingle.cpp
)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_library_clr(comfloat_wks
    STATIC
    ${FLOAT_SOURCES}
)

add_dependencies(comfloat_wks eventing_headers)
