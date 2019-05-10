add_library_clr(v3binder
    STATIC
    ${BINDER_SOURCES}
)

add_dependencies(v3binder eventing_headers)
