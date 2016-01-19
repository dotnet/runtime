include(${CLR_DIR}/crossgen.cmake)

add_library_clr(gcinfo_crossgen
    STATIC
    ${GCINFO_SOURCES}
)
