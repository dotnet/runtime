
include(${CLR_DIR}/dac.cmake)
include(../../md_dac.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIME_SOURCES)

if (WIN32)
    list(APPEND MDRUNTIME_SOURCES ${MDRUNTIME_HEADERS})
endif (WIN32)

add_library_clr(mdruntime_dac ${MDRUNTIME_SOURCES})