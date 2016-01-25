project(clrcompression)

if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include (GenerateExportHeader)

if($ENV{__BuildArch} STREQUAL x86 OR $ENV{__BuildArch} STREQUAL x64)
set(NATIVECOMPRESSION_SOURCES
    clrcompression.def
    zlib-intel/adler32.c
    zlib-intel/crc_folding.c
    zlib-intel/crc32.c
    zlib-intel/deflate_quick.c
    zlib-intel/deflate.c
    zlib-intel/fill_window_sse.c
    zlib-intel/match.c
    zlib-intel/trees.c
    zlib-intel/x86.c
    zlib-intel/zutil.c
    zlib/compress.c 
    zlib/inffast.c
    zlib/inflate.c
    zlib/inftrees.c
    #zlib/gzclose.c
    #zlib/gzlib.c
    #zlib/gzread.c
    #zlib/gzwrite.c
    #zlib/uncompr.c
    #zlib/infback.c
    #zlib/uncompr.c
)
endif()

if($ENV{__BuildArch} STREQUAL arm)
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
endif()

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
