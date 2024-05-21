# # IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

# set(ZLIB_SOURCES_BASE
#     adler32.c
#     compress.c
#     crc_folding.c
#     crc32.c
#     deflate_medium.c
#     deflate_quick.c
#     deflate.c
#     inffast.c
#     inflate.c
#     inftrees.c
#     match.c
#     slide_sse.c
#     trees.c
#     x86.c
#     zutil.c
#     ../../libs/System.IO.Compression.Native/zlib_allocator_win.c
# )

# addprefix(ZLIB_SOURCES "${CMAKE_CURRENT_LIST_DIR}/zlib-intel"  "${ZLIB_SOURCES_BASE}")

# # enable custom zlib allocator
# set(ZLIB_COMPILE_DEFINITIONS "MY_ZCALLOC")

# if(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
#     set(ZLIB_COMPILE_OPTIONS "/wd4127;/wd4131")
# endif()
