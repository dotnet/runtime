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

addprefix(ZLIB_SOURCES "${CMAKE_CURRENT_LIST_DIR}/zlib"  "${ZLIB_SOURCES_BASE}")

if (CMAKE_C_COMPILER_ID MATCHES "Clang")
  # Turn off warn-as-error for strict prototype checking until https://github.com/madler/zlib/issues/633 is fixed upstream
  set_source_files_properties(${ZLIB_SOURCES} PROPERTIES COMPILE_OPTIONS "-Wno-error=strict-prototypes")
endif()
