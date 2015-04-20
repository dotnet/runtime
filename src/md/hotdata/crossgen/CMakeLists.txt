include(${CLR_DIR}/crossgen.cmake)
include(../../md_wks.cmake)

add_precompiled_header(external.h ../external.cpp MDHOTDATA_SOURCES)
add_library(mdhotdata_crossgen ${MDHOTDATA_SOURCES})
