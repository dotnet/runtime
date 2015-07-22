
project(System.IO.Native)

set(NATIVEIO_SOURCES
    nativeio.h
    nativeio.cpp
)

add_library(System.IO.Native
    SHARED
    ${NATIVEIO_SOURCES}
)

install (TARGETS System.IO.Native DESTINATION .)
 
