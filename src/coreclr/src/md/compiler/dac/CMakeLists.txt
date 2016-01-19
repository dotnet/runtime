
include(${CLR_DIR}/dac.cmake)
include(../../md_dac.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDCOMPILER_SOURCES)
add_library_clr(mdcompiler_dac ${MDCOMPILER_SOURCES})
