include(${CLR_DIR}/crossgen.cmake)

# Add back some of the macro removed by crossgen.cmake.
add_definitions(
    -DEnC_SUPPORTED
    -DFEATURE_EVENT_TRACE
    -DFEATURE_MULTICOREJIT
    -DFEATURE_VERSIONING_LOG
)

add_definitions(-DCROSSGEN_MSCORLIB)

add_library_clr(mscorlib_crossgen ../mscorlib.cpp)
add_dependencies(cee_crossgen eventing_headers)
