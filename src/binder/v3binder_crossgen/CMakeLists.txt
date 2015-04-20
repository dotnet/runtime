include(${CLR_DIR}/crossgen.cmake)

add_library(v3binder_crossgen
    STATIC
    ${BINDER_SOURCES}
)
