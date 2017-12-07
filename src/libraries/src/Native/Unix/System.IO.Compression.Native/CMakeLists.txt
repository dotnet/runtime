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

add_library(System.IO.Compression.Native-Static
    STATIC
    ${NATIVECOMPRESSION_SOURCES}
    ${VERSION_FILE_PATH}
)

# Disable the "lib" prefix and override default name
set_target_properties(System.IO.Compression.Native-Static PROPERTIES PREFIX "")
set_target_properties(System.IO.Compression.Native-Static PROPERTIES OUTPUT_NAME System.IO.Compression.Native  CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(System.IO.Compression.Native
    ${ZLIB_LIBRARIES}
)

install_library_and_symbols (System.IO.Compression.Native)
install (TARGETS System.IO.Compression.Native-Static DESTINATION .)
