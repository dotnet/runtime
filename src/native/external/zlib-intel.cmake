set(ZLIB_SOURCES_BASE
    adler32.c
    compress.c
    crc_folding.c
    crc32.c
    deflate_medium.c
    deflate_quick.c
    deflate.c
    inffast.c
    inflate.c
    inftrees.c
    match.c
    slide_sse.c
    trees.c
    x86.c
    zutil.c
    ../../libs/System.IO.Compression.Native/zlib_allocator_win.c
)

# enable custom zlib allocator
add_definitions(-DMY_ZCALLOC)

addprefix(ZLIB_SOURCES "${CMAKE_CURRENT_LIST_DIR}/zlib-intel"  "${ZLIB_SOURCES_BASE}")
