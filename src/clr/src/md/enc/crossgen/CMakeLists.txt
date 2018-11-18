include(${CLR_DIR}/crossgen.cmake)
include(../../md_wks.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIMERW_SOURCES)

if (WIN32)
    list(APPEND MDRUNTIMERW_SOURCES ${MDRUNTIMERW_HEADERS})
endif (WIN32)

add_library_clr(mdruntimerw_crossgen ${MDRUNTIMERW_SOURCES})
