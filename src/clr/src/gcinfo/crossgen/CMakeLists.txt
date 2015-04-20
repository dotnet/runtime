include(${CLR_DIR}/crossgen.cmake)

add_library(gcinfo_crossgen
    STATIC
    ${GCINFO_SOURCES}
)
