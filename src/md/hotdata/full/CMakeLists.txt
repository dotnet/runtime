add_precompiled_header(external.h ../external.cpp MDHOTDATA_SOURCES)

add_library_clr(mdhotdata_full ${MDHOTDATA_SOURCES})
