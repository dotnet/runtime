// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       lz_decoder.h
/// \brief      LZ out window
///
//  Authors:    Igor Pavlov
//              Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_LZ_DECODER_H
#define LZMA_LZ_DECODER_H

#include "common.h"

#ifdef HAVE_IMMINTRIN_H
#	include <immintrin.h>
#endif


// dict_repeat() implementation variant:
// 0 = Byte-by-byte copying only.
// 1 = Use memcpy() for non-overlapping copies.
// 2 = Use x86 SSE2 for non-overlapping copies.
#ifndef LZMA_LZ_DECODER_CONFIG
#	if defined(TUKLIB_FAST_UNALIGNED_ACCESS) \
		&& defined(HAVE_IMMINTRIN_H) \
		&& (defined(__SSE2__) || defined(_M_X64) \
			|| (defined(_M_IX86_FP) && _M_IX86_FP >= 2))
#		define LZMA_LZ_DECODER_CONFIG 2
#	else
#		define LZMA_LZ_DECODER_CONFIG 1
#	endif
#endif

/// Byte-by-byte and memcpy() copy exactly the amount needed. Other methods
/// can copy up to LZ_DICT_EXTRA bytes more than requested, and this amount
/// of extra space is needed at the end of the allocated dictionary buffer.
///
/// NOTE: If this is increased, update LZMA_DICT_REPEAT_MAX too.
#if LZMA_LZ_DECODER_CONFIG >= 2
#	define LZ_DICT_EXTRA 32
#else
#	define LZ_DICT_EXTRA 0
#endif

/// Maximum number of bytes that dict_repeat() may copy. The allocated
/// dictionary buffer will be 2 * LZ_DICT_REPEAT_MAX + LZMA_DICT_EXTRA bytes
/// larger than the actual dictionary size:
///
/// (1) Every time the decoder reaches the end of the dictionary buffer,
///     the last LZ_DICT_REPEAT_MAX bytes will be copied to the beginning.
///     This way dict_repeat() will only need to copy from one place,
///     never from both the end and beginning of the buffer.
///
/// (2) The other LZ_DICT_REPEAT_MAX bytes is kept as a buffer between
///     the oldest byte still in the dictionary and the current write
///     position. This way dict_repeat() with the maximum valid distance
///     won't need memmove() as the copying cannot overlap.
///
/// (3) LZ_DICT_EXTRA bytes are required at the end of the dictionary buffer
///     so that extra copying done by dict_repeat() won't write or read past
///     the end of the allocated buffer. This amount is *not* counted as part
///     of lzma_dict.size.
///
/// Note that memcpy() still cannot be used if distance < len.
///
/// LZMA's longest match length is 273 bytes. The LZMA decoder looks at
/// the lowest four bits of the dictionary position, thus 273 must be
/// rounded up to the next multiple of 16 (288). In addition, optimized
/// dict_repeat() copies 32 bytes at a time, thus this must also be
/// a multiple of 32.
#define LZ_DICT_REPEAT_MAX 288

/// Initial position in lzma_dict.buf when the dictionary is empty.
#define LZ_DICT_INIT_POS (2 * LZ_DICT_REPEAT_MAX)


typedef struct {
	/// Pointer to the dictionary buffer.
	uint8_t *buf;

	/// Write position in dictionary. The next byte will be written to
	/// buf[pos].
	size_t pos;

	/// Indicates how full the dictionary is. This is used by
	/// dict_is_distance_valid() to detect corrupt files that would
	/// read beyond the beginning of the dictionary.
	size_t full;

	/// Write limit
	size_t limit;

	/// Allocated size of buf. This is 2 * LZ_DICT_REPEAT_MAX bytes
	/// larger than the actual dictionary size. This is enforced by
	/// how the value for "full" is set; it can be at most
	/// "size - 2 * LZ_DICT_REPEAT_MAX".
	size_t size;

	/// True once the dictionary has become full and the writing position
	/// has been wrapped in decode_buffer() in lz_decoder.c.
	bool has_wrapped;

	/// True when dictionary should be reset before decoding more data.
	bool need_reset;

} lzma_dict;


typedef struct {
	size_t dict_size;
	const uint8_t *preset_dict;
	size_t preset_dict_size;
} lzma_lz_options;


typedef struct {
	/// Data specific to the LZ-based decoder
	void *coder;

	/// Function to decode from in[] to *dict
	lzma_ret (*code)(void *coder,
			lzma_dict *restrict dict, const uint8_t *restrict in,
			size_t *restrict in_pos, size_t in_size);

	void (*reset)(void *coder, const void *options);

	/// Set the uncompressed size. If uncompressed_size == LZMA_VLI_UNKNOWN
	/// then allow_eopm will always be true.
	void (*set_uncompressed)(void *coder, lzma_vli uncompressed_size,
			bool allow_eopm);

	/// Free allocated resources
	void (*end)(void *coder, const lzma_allocator *allocator);

} lzma_lz_decoder;


#define LZMA_LZ_DECODER_INIT \
	(lzma_lz_decoder){ \
		.coder = NULL, \
		.code = NULL, \
		.reset = NULL, \
		.set_uncompressed = NULL, \
		.end = NULL, \
	}


extern lzma_ret lzma_lz_decoder_init(lzma_next_coder *next,
		const lzma_allocator *allocator,
		const lzma_filter_info *filters,
		lzma_ret (*lz_init)(lzma_lz_decoder *lz,
			const lzma_allocator *allocator,
			lzma_vli id, const void *options,
			lzma_lz_options *lz_options));

extern uint64_t lzma_lz_decoder_memusage(size_t dictionary_size);


//////////////////////
// Inline functions //
//////////////////////

/// Get a byte from the history buffer.
static inline uint8_t
dict_get(const lzma_dict *const dict, const uint32_t distance)
{
	return dict->buf[dict->pos - distance - 1
			+ (distance < dict->pos
				? 0 : dict->size - LZ_DICT_REPEAT_MAX)];
}


/// Optimized version of dict_get(dict, 0)
static inline uint8_t
dict_get0(const lzma_dict *const dict)
{
	return dict->buf[dict->pos - 1];
}


/// Test if dictionary is empty.
static inline bool
dict_is_empty(const lzma_dict *const dict)
{
	return dict->full == 0;
}


/// Validate the match distance
static inline bool
dict_is_distance_valid(const lzma_dict *const dict, const size_t distance)
{
	return dict->full > distance;
}


/// Repeat *len bytes at distance.
static inline bool
dict_repeat(lzma_dict *restrict dict,
		uint32_t distance, uint32_t *restrict len)
{
	// Don't write past the end of the dictionary.
	const size_t dict_avail = dict->limit - dict->pos;
	uint32_t left = my_min(dict_avail, *len);
	*len -= left;

	size_t back = dict->pos - distance - 1;
	if (distance >= dict->pos)
		back += dict->size - LZ_DICT_REPEAT_MAX;

#if LZMA_LZ_DECODER_CONFIG == 0
	// Minimal byte-by-byte method. This might be the least bad choice
	// if memcpy() isn't fast and there's no replacement for it below.
	while (left-- > 0) {
		dict->buf[dict->pos++] = dict->buf[back++];
	}

#else
	// Because memcpy() or a similar method can be faster than copying
	// byte by byte in a loop, the copying process is split into
	// two cases.
	if (distance < left) {
		// Source and target areas overlap, thus we can't use
		// memcpy() nor even memmove() safely.
		do {
			dict->buf[dict->pos++] = dict->buf[back++];
		} while (--left > 0);
	} else {
#	if LZMA_LZ_DECODER_CONFIG == 1
		memcpy(dict->buf + dict->pos, dict->buf + back, left);
		dict->pos += left;

#	elif LZMA_LZ_DECODER_CONFIG == 2
		// This can copy up to 32 bytes more than required.
		// (If left == 0, we still copy 32 bytes.)
		size_t pos = dict->pos;
		dict->pos += left;
		do {
			const __m128i x0 = _mm_loadu_si128(
					(__m128i *)(dict->buf + back));
			const __m128i x1 = _mm_loadu_si128(
					(__m128i *)(dict->buf + back + 16));
			back += 32;
			_mm_storeu_si128(
					(__m128i *)(dict->buf + pos), x0);
			_mm_storeu_si128(
					(__m128i *)(dict->buf + pos + 16), x1);
			pos += 32;
		} while (pos < dict->pos);

#	else
#		error "Invalid LZMA_LZ_DECODER_CONFIG value"
#	endif
	}
#endif

	// Update how full the dictionary is.
	if (!dict->has_wrapped)
		dict->full = dict->pos - LZ_DICT_INIT_POS;

	return *len != 0;
}


static inline void
dict_put(lzma_dict *restrict dict, uint8_t byte)
{
	dict->buf[dict->pos++] = byte;

	if (!dict->has_wrapped)
		dict->full = dict->pos - LZ_DICT_INIT_POS;
}


/// Puts one byte into the dictionary. Returns true if the dictionary was
/// already full and the byte couldn't be added.
static inline bool
dict_put_safe(lzma_dict *restrict dict, uint8_t byte)
{
	if (unlikely(dict->pos == dict->limit))
		return true;

	dict_put(dict, byte);
	return false;
}


/// Copies arbitrary amount of data into the dictionary.
static inline void
dict_write(lzma_dict *restrict dict, const uint8_t *restrict in,
		size_t *restrict in_pos, size_t in_size,
		size_t *restrict left)
{
	// NOTE: If we are being given more data than the size of the
	// dictionary, it could be possible to optimize the LZ decoder
	// so that not everything needs to go through the dictionary.
	// This shouldn't be very common thing in practice though, and
	// the slowdown of one extra memcpy() isn't bad compared to how
	// much time it would have taken if the data were compressed.

	if (in_size - *in_pos > *left)
		in_size = *in_pos + *left;

	*left -= lzma_bufcpy(in, in_pos, in_size,
			dict->buf, &dict->pos, dict->limit);

	if (!dict->has_wrapped)
		dict->full = dict->pos - LZ_DICT_INIT_POS;

	return;
}


static inline void
dict_reset(lzma_dict *dict)
{
	dict->need_reset = true;
	return;
}

#endif
