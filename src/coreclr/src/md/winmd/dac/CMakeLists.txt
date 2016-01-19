
include(${CLR_DIR}/dac.cmake)
include(../../md_dbi.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDWINMD_SOURCES)
add_library_clr(mdwinmd_dac ${MDWINMD_SOURCES})
