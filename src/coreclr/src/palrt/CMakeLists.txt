
set(CMAKE_INCLUDE_CURRENT_DIR ON)

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
    unicode.cpp
    variant.cpp
)

add_compile_options(-fPIC)

add_library_clr(palrt
    STATIC
    ${PALRT_SOURCES}
)

# Install the static PAL library for VS
install (TARGETS palrt DESTINATION lib)
