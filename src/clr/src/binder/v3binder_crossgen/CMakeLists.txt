include(${CLR_DIR}/crossgen.cmake)

add_library_clr(v3binder_crossgen
    STATIC
    ${BINDER_CROSSGEN_SOURCES}
)

add_dependencies(v3binder_crossgen eventing_headers)
