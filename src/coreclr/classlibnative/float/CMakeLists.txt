include_directories("../inc")

set(FLOAT_SOURCES
    floatdouble.cpp
    floatsingle.cpp
)

add_library_clr(comfloat_wks OBJECT ${FLOAT_SOURCES})
add_dependencies(comfloat_wks eventing_headers)
