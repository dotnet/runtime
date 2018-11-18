include(../../md_wks.cmake)

add_definitions(-DFEATURE_METADATA_EMIT_ALL)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIMERW_SOURCES)

if (WIN32)
    list(APPEND MDRUNTIMERW_SOURCES ${MDRUNTIMERW_HEADERS})
endif (WIN32)

add_library_clr(mdruntimerw_wks ${MDRUNTIMERW_SOURCES})
