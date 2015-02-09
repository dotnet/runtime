include(../../md_wks.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIME_SOURCES)
add_library(mdruntime_wks ${MDRUNTIME_SOURCES})

