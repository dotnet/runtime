set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(BCLTYPE_SOURCES
    arraynative.cpp
    arrayhelpers.cpp
    oavariant.cpp
    objectnative.cpp
    stringnative.cpp
    system.cpp
    varargsnative.cpp
    variant.cpp
)

add_library_clr(bcltype
    STATIC
    ${BCLTYPE_SOURCES}
)

add_dependencies(bcltype eventing_headers)
