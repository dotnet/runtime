set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories("../inc")

set(FLOAT_SOURCES
    floatdouble.cpp
    floatsingle.cpp
)

add_library_clr(comfloat_wks_obj
    OBJECT
    ${FLOAT_SOURCES}
)

add_dependencies(comfloat_wks_obj eventing_headers)

add_library(comfloat_wks INTERFACE)
target_sources(comfloat_wks INTERFACE $<TARGET_OBJECTS:comfloat_wks_obj>)