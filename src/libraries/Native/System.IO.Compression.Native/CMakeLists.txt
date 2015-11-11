project(System.IO.Compression.Native)

find_package(ZLIB REQUIRED)

set(NATIVECOMPRESSION_SOURCES
    pal_zlib.cpp
)

add_library(System.IO.Compression.Native
    SHARED
    ${NATIVECOMPRESSION_SOURCES}
)

target_link_libraries(System.IO.Compression.Native
    ${ZLIB_LIBRARIES}
)

install (TARGETS System.IO.Compression.Native DESTINATION .)
