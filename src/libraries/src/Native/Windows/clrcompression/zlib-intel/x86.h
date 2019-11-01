 /* x86.h -- check for CPU features
 * Copyright (C) 2013 Intel Corporation Jim Kukunas
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#ifndef X86_H
#define X86_H

#if defined(__x86_64__) || defined(_M_X64)
# define ZLIB_X86 64
#endif

#if defined(_M_IX86) || defined(__i386)
# define ZLIB_X86 32
#endif

#ifdef ZLIB_X86

#define UNALIGNED_OK
#define ADLER32_UNROLL_LESS
#define CRC32_UNROLL_LESS
#define USE_SSE_SLIDE
#define USE_CRC_HASH
#define USE_PCLMUL_CRC
#define USE_MEDIUM

extern int x86_cpu_has_sse2;
extern int x86_cpu_has_sse42;
extern int x86_cpu_has_pclmul;

void x86_check_features(void);

#if defined(USE_PCLMUL_CRC)
extern void crc_fold_init(unsigned crc[4 * 5]);
extern void crc_fold_copy(unsigned crc[4 * 5], unsigned char *dst, const unsigned char *src, long len);
extern unsigned crc_fold_512to32(unsigned crc[4 * 5]);
#endif

#endif
#endif
