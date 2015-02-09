add_precompiled_header(external.h ../external.cpp MDHOTDATA_SOURCES)

add_library(mdhotdata_full ${MDHOTDATA_SOURCES})
