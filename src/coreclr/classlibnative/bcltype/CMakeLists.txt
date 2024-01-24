set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(BCLTYPE_SOURCES
    arraynative.cpp
    oavariant.cpp
    objectnative.cpp
    system.cpp
    varargsnative.cpp
    variant.cpp
)

add_library_clr(bcltype OBJECT ${BCLTYPE_SOURCES})
add_dependencies(bcltype eventing_headers)
