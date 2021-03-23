set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(BCLTYPE_SOURCES
    arraynative.cpp
    oavariant.cpp
    objectnative.cpp
    stringnative.cpp
    system.cpp
    varargsnative.cpp
    variant.cpp
)

add_library_clr(bcltype_obj
    OBJECT
    ${BCLTYPE_SOURCES}
)

add_dependencies(bcltype_obj eventing_headers)
add_library(bcltype INTERFACE)
target_sources(bcltype INTERFACE $<TARGET_OBJECTS:bcltype_obj>)
