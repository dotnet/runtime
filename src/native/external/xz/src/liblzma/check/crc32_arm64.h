// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       crc32_arm64.h
/// \brief      CRC32 calculation with ARM64 optimization
//
//  Authors:    Chenxi Mao
//              Jia Tan
//              Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_CRC32_ARM64_H
#define LZMA_CRC32_ARM64_H

// MSVC always has the CRC intrinsics available when building for ARM64
// there is no need to include any header files.
#ifndef _MSC_VER
#	include <arm_acle.h>
#endif

// If both versions are going to be built, we need runtime detection
// to check if the instructions are supported.
#if defined(CRC32_GENERIC) && defined(CRC32_ARCH_OPTIMIZED)
#	if (defined(HAVE_GETAUXVAL) && defined(HAVE_HWCAP_CRC32)) \
			|| defined(HAVE_ELF_AUX_INFO)
#		include <sys/auxv.h>
#	elif defined(_WIN32)
#		include <processthreadsapi.h>
#	elif defined(__APPLE__) && defined(HAVE_SYSCTLBYNAME)
#		include <sys/sysctl.h>
#	endif
#endif

// Some EDG-based compilers support ARM64 and define __GNUC__
// (such as Nvidia's nvcc), but do not support function attributes.
//
// NOTE: Build systems check for this too, keep them in sync with this.
#if (defined(__GNUC__) || defined(__clang__)) && !defined(__EDG__)
#	define crc_attr_target __attribute__((__target__("+crc")))
#else
#	define crc_attr_target
#endif


crc_attr_target
static uint32_t
crc32_arch_optimized(const uint8_t *buf, size_t size, uint32_t crc)
{
	crc = ~crc;

	if (size >= 8) {
		// Align the input buffer because this was shown to be
		// significantly faster than unaligned accesses.
		const size_t align = (0 - (uintptr_t)buf) & 7;

		if (align & 1)
			crc = __crc32b(crc, *buf++);

		if (align & 2) {
			crc = __crc32h(crc, aligned_read16le(buf));
			buf += 2;
		}

		if (align & 4) {
			crc = __crc32w(crc, aligned_read32le(buf));
			buf += 4;
		}

		size -= align;

		// Process 8 bytes at a time. The end point is determined by
		// ignoring the least significant three bits of size to
		// ensure we do not process past the bounds of the buffer.
		// This guarantees that limit is a multiple of 8 and is
		// strictly less than size.
		for (const uint8_t *limit = buf + (size & ~(size_t)7);
				buf < limit; buf += 8)
			crc = __crc32d(crc, aligned_read64le(buf));

		size &= 7;
	}

	// Process the remaining bytes that are not 8 byte aligned.
	if (size & 4) {
		crc = __crc32w(crc, aligned_read32le(buf));
		buf += 4;
	}

	if (size & 2) {
		crc = __crc32h(crc, aligned_read16le(buf));
		buf += 2;
	}

	if (size & 1)
		crc = __crc32b(crc, *buf);

	return ~crc;
}


#if defined(CRC32_GENERIC) && defined(CRC32_ARCH_OPTIMIZED)
static inline bool
is_arch_extension_supported(void)
{
#if defined(HAVE_GETAUXVAL) && defined(HAVE_HWCAP_CRC32)
	return (getauxval(AT_HWCAP) & HWCAP_CRC32) != 0;

#elif defined(HAVE_ELF_AUX_INFO)
	unsigned long feature_flags;

	if (elf_aux_info(AT_HWCAP, &feature_flags, sizeof(feature_flags)) != 0)
		return false;

	return (feature_flags & HWCAP_CRC32) != 0;

#elif defined(_WIN32)
	return IsProcessorFeaturePresent(
			PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE);

#elif defined(__APPLE__) && defined(HAVE_SYSCTLBYNAME)
	int has_crc32 = 0;
	size_t size = sizeof(has_crc32);

	// The sysctlbyname() function requires a string identifier for the
	// CPU feature it tests. The Apple documentation lists the string
	// "hw.optional.armv8_crc32", which can be found here:
	// https://developer.apple.com/documentation/kernel/1387446-sysctlbyname/determining_instruction_set_characteristics#3915619
	if (sysctlbyname("hw.optional.armv8_crc32", &has_crc32,
			&size, NULL, 0) != 0)
		return false;

	return has_crc32;

#else
	// If a runtime detection method cannot be found, then this must
	// be a compile time error. The checks in crc_common.h should ensure
	// a runtime detection method is always found if this function is
	// built. It would be possible to just return false here, but this
	// is inefficient for binary size and runtime since only the generic
	// method could ever be used.
#	error Runtime detection method unavailable.
#endif
}
#endif

#endif // LZMA_CRC32_ARM64_H
