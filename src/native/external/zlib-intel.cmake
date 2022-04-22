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
)

addprefix(ZLIB_SOURCES "${CMAKE_CURRENT_LIST_DIR}/zlib-intel"  "${ZLIB_SOURCES_BASE}")

function(set_zlib_source_files_properties)
if (CMAKE_C_COMPILER_ID MATCHES "Clang")
  # Turn off warn-as-error for strict prototype checking until https://github.com/madler/zlib/issues/633 is fixed upstream
  set_source_files_properties(${ZLIB_SOURCES} PROPERTIES COMPILE_OPTIONS "-Wno-error=strict-prototypes;-Wno-error=missing-prototypes")
endif()
endfunction()
