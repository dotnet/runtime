// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       crc_common.h
/// \brief      Macros and declarations for CRC32 and CRC64
//
//  Authors:    Lasse Collin
//              Ilya Kurdyukov
//              Jia Tan
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_CRC_COMMON_H
#define LZMA_CRC_COMMON_H

#include "common.h"


/////////////
// Generic //
/////////////

#ifdef WORDS_BIGENDIAN
#	define A(x) ((x) >> 24)
#	define B(x) (((x) >> 16) & 0xFF)
#	define C(x) (((x) >> 8) & 0xFF)
#	define D(x) ((x) & 0xFF)

#	define S8(x) ((x) << 8)
#	define S32(x) ((x) << 32)

#else
#	define A(x) ((x) & 0xFF)
#	define B(x) (((x) >> 8) & 0xFF)
#	define C(x) (((x) >> 16) & 0xFF)
#	define D(x) ((x) >> 24)

#	define S8(x) ((x) >> 8)
#	define S32(x) ((x) >> 32)
#endif


/// lzma_crc32_table[0] is needed by LZ encoder so we need to keep
/// the array two-dimensional.
#ifdef HAVE_SMALL
lzma_attr_visibility_hidden
extern uint32_t lzma_crc32_table[1][256];

extern void lzma_crc32_init(void);

#else

lzma_attr_visibility_hidden
extern const uint32_t lzma_crc32_table[8][256];

lzma_attr_visibility_hidden
extern const uint64_t lzma_crc64_table[4][256];
#endif


///////////////////
// Configuration //
///////////////////

// NOTE: This config isn't used if HAVE_SMALL is defined!

// These are defined if the generic slicing-by-n implementations and their
// lookup tables are built.
#undef CRC32_GENERIC
#undef CRC64_GENERIC

// These are defined if an arch-specific version is built. If both this
// and matching _GENERIC is defined then runtime detection must be used.
#undef CRC32_ARCH_OPTIMIZED
#undef CRC64_ARCH_OPTIMIZED

// The x86 CLMUL is used for both CRC32 and CRC64.
#undef CRC_X86_CLMUL

// Many ARM64 processor have CRC32 instructions.
// CRC64 could be done with CLMUL but it's not implemented yet.
#undef CRC32_ARM64

// 64-bit LoongArch has CRC32 instructions.
#undef CRC32_LOONGARCH


// ARM64
//
// Keep this in sync with changes to crc32_arm64.h
#if defined(_WIN32) \
		|| (defined(HAVE_GETAUXVAL) && defined(HAVE_HWCAP_CRC32)) \
		|| defined(HAVE_ELF_AUX_INFO) \
		|| (defined(__APPLE__) && defined(HAVE_SYSCTLBYNAME))
#	define CRC_ARM64_RUNTIME_DETECTION 1
#endif

// ARM64 CRC32 instruction is only useful for CRC32. Currently, only
// little endian is supported since we were unable to test on a big
// endian machine.
#if defined(HAVE_ARM64_CRC32) && !defined(WORDS_BIGENDIAN)
	// Allow ARM64 CRC32 instruction without a runtime check if
	// __ARM_FEATURE_CRC32 is defined. GCC and Clang only define
	// this if the proper compiler options are used.
#	if defined(__ARM_FEATURE_CRC32)
#		define CRC32_ARCH_OPTIMIZED 1
#		define CRC32_ARM64 1
#	elif defined(CRC_ARM64_RUNTIME_DETECTION)
#		define CRC32_ARCH_OPTIMIZED 1
#		define CRC32_ARM64 1
#		define CRC32_GENERIC 1
#	endif
#endif


// LoongArch
//
// Only 64-bit LoongArch is supported for now. No runtime detection
// is needed because the LoongArch specification says that the CRC32
// instructions are a part of the Basic Integer Instructions and
// they shall be implemented by 64-bit LoongArch implementations.
#ifdef HAVE_LOONGARCH_CRC32
#	define CRC32_ARCH_OPTIMIZED 1
#	define CRC32_LOONGARCH 1
#endif


// x86 and E2K
#if defined(HAVE_USABLE_CLMUL)
	// If CLMUL is allowed unconditionally in the compiler options then
	// the generic version and the tables can be omitted. Exceptions:
	//
	//   - If 32-bit x86 assembly files are enabled then those are always
	//     built and runtime detection is used even if compiler flags
	//     were set to allow CLMUL unconditionally.
	//
	//   - The unconditional use doesn't work with MSVC as I don't know
	//     how to detect the features here.
	//
	// Don't enable CLMUL at all on old MSVC that targets 32-bit x86.
	// There seems to be a compiler bug that produces broken code
	// in optimized (Release) builds. It results in crashing tests.
	// It is known that VS 2019 16.11 (MSVC 19.29.30158) is broken
	// and that VS 2022 17.13 (MSVC 19.43.34808) works.
#	if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 194334808 \
			&& !defined(__INTEL_COMPILER) && !defined(__clang__) \
			&& defined(_M_IX86)
		// Old MSVC targeting 32-bit x86: Don't enable CLMUL at all.
#	elif (defined(__SSSE3__) && defined(__SSE4_1__) \
			&& defined(__PCLMUL__) \
			&& !defined(HAVE_CRC_X86_ASM)) \
		|| (defined(__e2k__) && __iset__ >= 6)
#		define CRC32_ARCH_OPTIMIZED 1
#		define CRC64_ARCH_OPTIMIZED 1
#		define CRC_X86_CLMUL 1
#	else
#		define CRC32_GENERIC 1
#		define CRC64_GENERIC 1
#		define CRC32_ARCH_OPTIMIZED 1
#		define CRC64_ARCH_OPTIMIZED 1
#		define CRC_X86_CLMUL 1
#	endif
#endif


// Fallback configuration
//
// For CRC32 use the generic slice-by-eight implementation if no optimized
// version is available.
#if !defined(CRC32_ARCH_OPTIMIZED) && !defined(CRC32_GENERIC)
#	define CRC32_GENERIC 1
#endif

// For CRC64 use the generic slice-by-four implementation if no optimized
// version is available.
#if !defined(CRC64_ARCH_OPTIMIZED) && !defined(CRC64_GENERIC)
#	define CRC64_GENERIC 1
#endif

#endif
