
include(${CLR_DIR}/dac.cmake)

add_library_clr(strongname_dac ${STRONGNAME_SOURCES})
add_dependencies(strongname_dac eventing_headers)
