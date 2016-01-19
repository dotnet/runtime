
include(${CLR_DIR}/dac.cmake)

add_precompiled_header(external.h ../external.cpp MDHOTDATA_SOURCES)

add_library_clr(mdhotdata_dac ${MDHOTDATA_SOURCES})
