project(clrcompression)

if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include (GenerateExportHeader)

set(NATIVECOMPRESSION_SOURCES
    clrcompression.def
    zlib/adler32.c
    zlib/compress.c
    zlib/crc32.c
    zlib/deflate.c
    zlib/inffast.c
    zlib/inflate.c
    zlib/inftrees.c
    zlib/trees.c
    zlib/zutil.c
    #zlib/gzio.c
    #zlib/infback.c
    #zlib/uncompr.c
)

if ($ENV{__VSVersion} STREQUAL vs2015)
    add_definitions(-DHAVE_VSNPRINTF)
endif ()

add_library(clrcompression
    SHARED
    ${NATIVECOMPRESSION_SOURCES}
)

GENERATE_EXPORT_HEADER( clrcompression
     BASE_NAME clrcompression
     EXPORT_MACRO_NAME clrcompression_EXPORT
     EXPORT_FILE_NAME clrcompression_Export.h
     STATIC_DEFINE clrcompression_BUILT_AS_STATIC
)

install (TARGETS clrcompression DESTINATION .)
