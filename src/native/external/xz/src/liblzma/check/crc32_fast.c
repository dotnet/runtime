// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       crc32_fast.c
/// \brief      CRC32 calculation
//
//  Authors:    Lasse Collin
//              Ilya Kurdyukov
//
///////////////////////////////////////////////////////////////////////////////

#include "check.h"
#include "crc_common.h"

#if defined(CRC_X86_CLMUL)
#	define BUILDING_CRC_CLMUL 32
#	include "crc_x86_clmul.h"
#elif defined(CRC32_ARM64)
#	include "crc32_arm64.h"
#elif defined(CRC32_LOONGARCH)
#	include "crc32_loongarch.h"
#endif


#ifdef CRC32_GENERIC

///////////////////
// Generic CRC32 //
///////////////////

#ifdef WORDS_BIGENDIAN
#	include "crc32_table_be.h"
#else
#	include "crc32_table_le.h"
#endif


#ifdef HAVE_CRC_X86_ASM
extern uint32_t lzma_crc32_generic(
		const uint8_t *buf, size_t size, uint32_t crc);
#else
static uint32_t
lzma_crc32_generic(const uint8_t *buf, size_t size, uint32_t crc)
{
	crc = ~crc;

#ifdef WORDS_BIGENDIAN
	crc = byteswap32(crc);
#endif

	if (size > 8) {
		// Fix the alignment, if needed. The if statement above
		// ensures that this won't read past the end of buf[].
		while ((uintptr_t)(buf) & 7) {
			crc = lzma_crc32_table[0][*buf++ ^ A(crc)] ^ S8(crc);
			--size;
		}

		// Calculate the position where to stop.
		const uint8_t *const limit = buf + (size & ~(size_t)(7));

		// Calculate how many bytes must be calculated separately
		// before returning the result.
		size &= (size_t)(7);

		// Calculate the CRC32 using the slice-by-eight algorithm.
		while (buf < limit) {
			crc ^= aligned_read32ne(buf);
			buf += 4;

			crc = lzma_crc32_table[7][A(crc)]
			    ^ lzma_crc32_table[6][B(crc)]
			    ^ lzma_crc32_table[5][C(crc)]
			    ^ lzma_crc32_table[4][D(crc)];

			const uint32_t tmp = aligned_read32ne(buf);
			buf += 4;

			// At least with some compilers, it is critical for
			// performance, that the crc variable is XORed
			// between the two table-lookup pairs.
			crc = lzma_crc32_table[3][A(tmp)]
			    ^ lzma_crc32_table[2][B(tmp)]
			    ^ crc
			    ^ lzma_crc32_table[1][C(tmp)]
			    ^ lzma_crc32_table[0][D(tmp)];
		}
	}

	while (size-- != 0)
		crc = lzma_crc32_table[0][*buf++ ^ A(crc)] ^ S8(crc);

#ifdef WORDS_BIGENDIAN
	crc = byteswap32(crc);
#endif

	return ~crc;
}
#endif // HAVE_CRC_X86_ASM
#endif // CRC32_GENERIC


#if defined(CRC32_GENERIC) && defined(CRC32_ARCH_OPTIMIZED)

//////////////////////////
// Function dispatching //
//////////////////////////

// If both the generic and arch-optimized implementations are built, then
// the function to use is selected at runtime because the system running
// the binary might not have the arch-specific instruction set extension(s)
// available. The dispatch methods in order of priority:
//
// 1. Constructor. This method uses __attribute__((__constructor__)) to
//    set crc32_func at load time. This avoids extra computation (and any
//    unlikely threading bugs) on the first call to lzma_crc32() to decide
//    which implementation should be used.
//
// 2. First Call Resolution. On the very first call to lzma_crc32(), the
//    call will be directed to crc32_dispatch() instead. This will set the
//    appropriate implementation function and will not be called again.
//    This method does not use any kind of locking but is safe because if
//    multiple threads run the dispatcher simultaneously then they will all
//    set crc32_func to the same value.

typedef uint32_t (*crc32_func_type)(
		const uint8_t *buf, size_t size, uint32_t crc);

// This resolver is shared between all dispatch methods.
static crc32_func_type
crc32_resolve(void)
{
	return is_arch_extension_supported()
			? &crc32_arch_optimized : &lzma_crc32_generic;
}


#ifdef HAVE_FUNC_ATTRIBUTE_CONSTRUCTOR
// Constructor method.
#	define CRC32_SET_FUNC_ATTR __attribute__((__constructor__))
static crc32_func_type crc32_func;
#else
// First Call Resolution method.
#	define CRC32_SET_FUNC_ATTR
static uint32_t crc32_dispatch(const uint8_t *buf, size_t size, uint32_t crc);
static crc32_func_type crc32_func = &crc32_dispatch;
#endif

CRC32_SET_FUNC_ATTR
static void
crc32_set_func(void)
{
	crc32_func = crc32_resolve();
	return;
}

#ifndef HAVE_FUNC_ATTRIBUTE_CONSTRUCTOR
static uint32_t
crc32_dispatch(const uint8_t *buf, size_t size, uint32_t crc)
{
	// When __attribute__((__constructor__)) isn't supported, set the
	// function pointer without any locking. If multiple threads run
	// the detection code in parallel, they will all end up setting
	// the pointer to the same value. This avoids the use of
	// mythread_once() on every call to lzma_crc32() but this likely
	// isn't strictly standards compliant. Let's change it if it breaks.
	crc32_set_func();
	return crc32_func(buf, size, crc);
}

#endif
#endif


extern LZMA_API(uint32_t)
lzma_crc32(const uint8_t *buf, size_t size, uint32_t crc)
{
#if defined(CRC32_GENERIC) && defined(CRC32_ARCH_OPTIMIZED)
/*
#ifndef HAVE_FUNC_ATTRIBUTE_CONSTRUCTOR
	// See crc32_dispatch(). This would be the alternative which uses
	// locking and doesn't use crc32_dispatch(). Note that on Windows
	// this method needs Vista threads.
	mythread_once(crc64_set_func);
#endif
*/
	return crc32_func(buf, size, crc);

#elif defined(CRC32_ARCH_OPTIMIZED)
	return crc32_arch_optimized(buf, size, crc);

#else
	return lzma_crc32_generic(buf, size, crc);
#endif
}
