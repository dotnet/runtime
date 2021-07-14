
set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(PALRT_SOURCES
    bstr.cpp
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

set(CLR_CMAKE_ENABLE_ASAN OFF)

add_library_clr(palrt_nosanitizer
    STATIC
    ${PALRT_SOURCES}
)

# Install the static PAL library for VS
install_clr(TARGETS palrt DESTINATIONS lib)
install_clr(TARGETS palrt_nosanitizer DESTINATIONS lib)
