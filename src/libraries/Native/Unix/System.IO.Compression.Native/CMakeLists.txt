project(System.IO.Compression.Native)

find_package(ZLIB REQUIRED)

set(NATIVECOMPRESSION_SOURCES
    pal_zlib.cpp
)

add_library(System.IO.Compression.Native
    SHARED
    ${NATIVECOMPRESSION_SOURCES}
    ${VERSION_FILE_PATH}
)

target_link_libraries(System.IO.Compression.Native
    ${ZLIB_LIBRARIES}
)

install_library_and_symbols (System.IO.Compression.Native)
