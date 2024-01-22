# IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

set(ZLIB_SOURCES_BASE
    adler32.c
    compress.c
    crc32.c
    uncompr.c
    deflate.c
    gzguts.h
    trees.c
    zutil.c
    inflate.c
    infback.c
    inftrees.c
    inffast.c
    crc32.h
    deflate.h
    inffast.h
    inffixed.h
    inflate.h
    inftrees.h
    trees.h
    zconf.h
    zlib.h
    zutil.h
)

if(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
    set(ZLIB_SOURCES_BASE ${ZLIB_SOURCES_BASE} ../../libs/System.IO.Compression.Native/zlib_allocator_win.c)
else()
    set(ZLIB_SOURCES_BASE ${ZLIB_SOURCES_BASE} ../../libs/System.IO.Compression.Native/zlib_allocator_unix.c)
endif()

addprefix(ZLIB_SOURCES "${CMAKE_CURRENT_LIST_DIR}/zlib"  "${ZLIB_SOURCES_BASE}")

# enable custom zlib allocator
set(ZLIB_COMPILE_DEFINITIONS "MY_ZCALLOC")

if(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
    set(ZLIB_COMPILE_OPTIONS "/wd4127;/wd4131")
endif()
