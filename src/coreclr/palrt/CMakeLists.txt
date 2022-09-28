
set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(PALRT_SOURCES
    coguid.cpp
    comem.cpp
    guid.cpp
    memorystream.cpp
    path.cpp
    variant.cpp
)

add_library_clr(palrt
    STATIC
    ${PALRT_SOURCES}
)

# Install the static PAL library for VS
install_clr(TARGETS palrt DESTINATIONS lib)
