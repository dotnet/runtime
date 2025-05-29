/* s390_functions.h -- s390 implementations for arch-specific functions.
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#ifndef S390_FUNCTIONS_H_
#define S390_FUNCTIONS_H_

#ifdef S390_CRC32_VX
uint32_t crc32_s390_vx(uint32_t crc, const uint8_t *buf, size_t len);

#ifdef __clang__
#  if ((__clang_major__ == 18) || (__clang_major__ == 19 && (__clang_minor__ < 1 || (__clang_minor__ == 1 && __clang_patchlevel__ < 2))))
# error CRC32-VX optimizations are broken due to compiler bug in Clang versions: 18.0.0 <= clang_version < 19.1.2. \
        Either disable the zlib-ng CRC32-VX optimization, or switch to another compiler/compiler version.
#  endif
#endif

#endif

#ifdef DISABLE_RUNTIME_CPU_DETECTION
#  if defined(S390_CRC32_VX) && defined(__zarch__) && __ARCH__ >= 11 && defined(__VX__)
#    undef native_crc32
#    define native_crc32 = crc32_s390_vx
#  endif
#endif

#endif
