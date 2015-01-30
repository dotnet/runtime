add_definitions(-D_CRTIMP=) # static link of crt
add_library(mdhotdata-staticcrt ${MDHOTDATA_SOURCES})
