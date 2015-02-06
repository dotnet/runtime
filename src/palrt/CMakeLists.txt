
set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DNO_CRT)

set(PALRT_SOURCES
    bstr.cpp
    coguid.cpp
    comem.cpp
    decarith.cpp
    decconv.cpp
    guid.cpp
    memorystream.cpp
    path.cpp
    urlpars.cpp
    variant.cpp
)

add_compile_options(-fPIC)

add_library(palrt
    STATIC
    ${PALRT_SOURCES}
)
