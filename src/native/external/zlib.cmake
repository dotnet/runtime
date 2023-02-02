if(MSVC)
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4244>) # conversion from 'type1' to 'type2', possible loss of data
else(CMAKE_C_COMPILER_ID MATCHES "Clang")
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:-Wno-implicit-int-conversion>)
endif()

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

# Configure our custom allocator
if(MSVC)
    set(ZLIB_SOURCES_BASE ${ZLIB_SOURCES_BASE} dotnet_allocator_win.c)
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/DMY_ZCALLOC>)
else()
    set(ZLIB_SOURCES_BASE ${ZLIB_SOURCES_BASE} dotnet_allocator_unix.c)
    add_definitions(-DMY_ZCALLOC)
endif()

addprefix(ZLIB_SOURCES "${CMAKE_CURRENT_LIST_DIR}/zlib"  "${ZLIB_SOURCES_BASE}")
