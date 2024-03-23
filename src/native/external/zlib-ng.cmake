# IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

set(ZLIB_NG_SOURCES_BASE
# Base *.c
    adler32_fold.c
    adler32.c
    chunkset.c
    compare256.c
    compress.c
    cpu_features.c
    crc32_braid_comb.c
    crc32_braid.c
    crc32_fold.c
    deflate_fast.c
    deflate_huff.c
    deflate_medium.c
    deflate_quick.c
    deflate_rle.c
    deflate_slow.c
    deflate_stored.c
    deflate.c
    functable.c
    gzlib.c
    gzwrite.c
    infback.c
    inflate.c
    inftrees.c
    insert_string_roll.c
    insert_string.c
    slide_hash.c
    trees.c
    uncompr.c
    zutil.c
    adler32_fold.h
# Base *.h
    adler32_p.h
    chunkset_tpl.h
    compare256_rle.h
    cpu_features.h
    crc32_braid_comb_p.h
    crc32_braid_p.h
    crc32_braid_tbl.h
    crc32_fold.h
    deflate_p.h
    deflate.h
    fallback_builtins.h
    functable.h
    gzguts.h
    inffast_tpl.h
    inffixed_tbl.h
    inflate_p.h
    inflate.h
    inftrees.h
    insert_string_tpl.h
    match_tpl.h
    trees_emit.h
    trees_tbl.h
    trees.h
    zbuild.h
    zendian.h
    zutil_p.h
    zutil.h
# ARM *.c
    arch/arm/adler32_neon.c
    arch/arm/arm_features.c
    arch/arm/chunkset_neon.c
    arch/arm/compare256_neon.c
    arch/arm/crc32_acle.c
    arch/arm/insert_string_acle.c
    arch/arm/slide_hash_armv6.c
    arch/arm/slide_hash_neon.c
# ARM *.h
    arch/arm/acle_intrins.h
    arch/arm/arm_features.h
    arch/arm/neon_intrins.h
# Power *.c
    arch/power/adler32_power8.c
    arch/power/adler32_vmx.c
    arch/power/chunkset_power8.c
    arch/power/compare256_power9.c
    arch/power/crc32_power8.c
    arch/power/power_features.c
    arch/power/slide_hash_power8.c
    arch/power/slide_hash_vmx.c
# Power *.h
    arch/power/crc32_constants.h
    arch/power/fallback_builtins.h
    arch/power/power_features.h
    arch/power/slide_ppc_tpl.h
# RISCV *.c
    arch/riscv/adler32_rvv.c
    arch/riscv/chunkset_rvv.c
    arch/riscv/compare256_rvv.c
    arch/riscv/riscv_features.c
    arch/riscv/slide_hash_rvv.c
# RISCV *.h
    arch/riscv/riscv_features.h
# S390 *.c
    arch/s390/crc32-vx.c
    arch/s390/dfltcc_common.c
    arch/s390/dfltcc_deflate.c
    arch/s390/dfltcc_inflate.c
    arch/s390/s390_features.c
# S390 *.h
    arch/s390/dfltcc_common.h
    arch/s390/dfltcc_deflate.h
    arch/s390/dfltcc_detail.h
    arch/s390/dfltcc_inflate.h
    arch/s390/s390_features.h
# X86 *.c
    arch/x86/adler32_avx2.c
    arch/x86/adler32_avx512_vnni.c
    arch/x86/adler32_avx512.c
    arch/x86/adler32_sse42.c
    arch/x86/adler32_ssse3.c
    arch/x86/chunkset_avx2.c
    arch/x86/chunkset_sse2.c
    arch/x86/chunkset_ssse3.c
    arch/x86/compare256_avx2.c
    arch/x86/compare256_sse2.c
    arch/x86/crc32_pclmulqdq.c
    arch/x86/crc32_vpclmulqdq.c
    arch/x86/insert_string_sse42.c
    arch/x86/slide_hash_avx2.c
    arch/x86/slide_hash_sse2.c
    arch/x86/x86_features.c
# X86 *.h
    arch/x86/adler32_avx2_p.h
    arch/x86/adler32_avx512_p.h
    arch/x86/adler32_ssse3_p.h
    arch/x86/crc32_fold_pclmulqdq_tpl.h
    arch/x86/crc32_fold_vpclmulqdq_tpl.h
    arch/x86/crc32_pclmulqdq_tpl.h
    arch/x86/x86_features.h
    arch/x86/x86_intrins.h
# Generic *.h
    arch/generic/chunk_permute_table.h
)

if(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
    set(ZLIB_NG_SOURCES_BASE ${ZLIB_NG_SOURCES_BASE} ../../libs/System.IO.Compression.Native/zlib_ng_allocator_win.c)
else()
    set(ZLIB_NG_SOURCES_BASE ${ZLIB_NG_SOURCES_BASE} ../../libs/System.IO.Compression.Native/zlib_ng_allocator_unix.c)
endif()

addprefix(ZLIB_NG_SOURCES "${CMAKE_CURRENT_LIST_DIR}/zlib-ng"  "${ZLIB_NG_SOURCES_BASE}")

# enable custom zlib allocator
set(ZLIB_NG_COMPILE_DEFINITIONS "MY_ZCALLOC")

# Compile for zlib-compatible APIs instead of zlib-ng APIs
set(ZLIB_COMPILE_OPTIONS "/zlib-compat")

if(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
    set(ZLIB_COMPILE_OPTIONS "${ZLIB_COMPILE_OPTIONS};/wd4127;/wd4131")
endif()