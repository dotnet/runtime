include(../../md_wks.cmake)

add_definitions(-DFEATURE_METADATA_EMIT_ALL)

add_precompiled_header(stdafx.h ../stdafx.cpp MDWINMD_SOURCES)

if (WIN32)
    list(APPEND MDWINMD_SOURCES ${MDWINMD_HEADERS})
endif (WIN32)

add_library_clr(mdwinmd_wks ${MDWINMD_SOURCES})
