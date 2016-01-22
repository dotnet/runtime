include(${CLR_DIR}/crossgen.cmake)
include(../../md_wks.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIMERW_SOURCES)
add_library_clr(mdruntimerw_crossgen ${MDRUNTIMERW_SOURCES})
