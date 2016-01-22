add_definitions(-D_CRTIMP=) # static link of crt

add_precompiled_header(external.h ../external.cpp MDHOTDATA_SOURCES)
add_library_clr(mdhotdata-staticcrt ${MDHOTDATA_SOURCES})
