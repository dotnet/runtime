
set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(PALRT_SOURCES
    memorystream.cpp
    variant.cpp
)

add_library_clr(palrt
    STATIC
    ${PALRT_SOURCES}
)

target_link_libraries(palrt PUBLIC minipal)
