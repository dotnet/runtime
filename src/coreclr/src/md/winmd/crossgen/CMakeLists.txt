include(${CLR_DIR}/crossgen.cmake)
include(../../md_wks.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDWINMD_SOURCES)

if (WIN32)
    list(APPEND MDWINMD_SOURCES ${MDWINMD_HEADERS})
endif (WIN32)

add_library_clr(mdwinmd_crossgen ${MDWINMD_SOURCES})
