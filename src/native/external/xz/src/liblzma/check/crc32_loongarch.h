// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       crc32_loongarch.h
/// \brief      CRC32 calculation with LoongArch optimization
//
//  Authors:    Xi Ruoyao
//              Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_CRC32_LOONGARCH_H
#define LZMA_CRC32_LOONGARCH_H

#include <larchintrin.h>


static uint32_t
crc32_arch_optimized(const uint8_t *buf, size_t size, uint32_t crc_unsigned)
{
	int32_t crc = (int32_t)~crc_unsigned;

	if (size >= 8) {
		const size_t align = (0 - (uintptr_t)buf) & 7;

		if (align & 1)
			crc = __crc_w_b_w((int8_t)*buf++, crc);

		if (align & 2) {
			crc = __crc_w_h_w((int16_t)aligned_read16le(buf), crc);
			buf += 2;
		}

		if (align & 4) {
			crc = __crc_w_w_w((int32_t)aligned_read32le(buf), crc);
			buf += 4;
		}

		size -= align;

		for (const uint8_t *limit = buf + (size & ~(size_t)7);
				buf < limit; buf += 8)
			crc = __crc_w_d_w((int64_t)aligned_read64le(buf), crc);

		size &= 7;
	}

	if (size & 4) {
		crc = __crc_w_w_w((int32_t)aligned_read32le(buf), crc);
		buf += 4;
	}

	if (size & 2) {
		crc = __crc_w_h_w((int16_t)aligned_read16le(buf), crc);
		buf += 2;
	}

	if (size & 1)
		crc = __crc_w_b_w((int8_t)*buf, crc);

	return (uint32_t)~crc;
}

#endif // LZMA_CRC32_LOONGARCH_H
