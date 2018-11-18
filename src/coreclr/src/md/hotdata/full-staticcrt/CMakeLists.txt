add_definitions(-D_CRTIMP=) # static link of crt

add_precompiled_header(external.h ../external.cpp MDHOTDATA_SOURCES)

if (WIN32)
    list(APPEND MDHOTDATA_SOURCES ${MDHOTDATA_HEADERS})
endif (WIN32)

add_library_clr(mdhotdata-staticcrt ${MDHOTDATA_SOURCES})
