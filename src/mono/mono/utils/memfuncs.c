/**
 * \file
 * Our own bzero/memmove.
 *
 * Copyright (C) 2013-2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * SGen cannot deal with invalid pointers on the heap or in registered roots.  Sometimes we
 * need to copy or zero out memory in code that might be interrupted by collections.  To
 * guarantee that those operations will not result in invalid pointers, we must do it
 * word-atomically.
 *
 * libc's bzero() and memcpy()/memmove() functions do not guarantee word-atomicity, even in
 * cases where one would assume so.  For instance, some implementations (like Darwin's on
 * x86) have variants of memcpy() using vector instructions.  Those may copy bytewise for
 * the region preceding the first vector-aligned address.  That region could be
 * word-aligned, but it would still be copied byte-wise.
 *
 * All our memory writes here are to "volatile" locations.  This is so that C compilers
 * don't "optimize" our code back to calls to bzero()/memmove().  LLVM, specifically, will
 * do that.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include "memfuncs.h"

#define ptr_mask ((sizeof (void*) - 1))
#define _toi(ptr) ((size_t)ptr)
#define unaligned_bytes(ptr) (_toi(ptr) & ptr_mask)
#define align_down(ptr) ((void*)(_toi(ptr) & ~ptr_mask))
#define align_up(ptr) ((void*) ((_toi(ptr) + ptr_mask) & ~ptr_mask))
#if SIZEOF_VOID_P == 4
#define bytes_to_words(n)	((size_t)(n) >> 2)
#elif SIZEOF_VOID_P == 8
#define bytes_to_words(n)	((size_t)(n) >> 3)
#else
#error We only support 32 and 64 bit architectures.
#endif

#define BZERO_WORDS(dest,words) do {			\
		void * volatile *__d = (void* volatile*)(dest);		\
		int __n = (words);			\
		int __i;				\
		for (__i = 0; __i < __n; ++__i)		\
			__d [__i] = NULL;		\
	} while (0)


/**
 * mono_gc_bzero_aligned:
 * \param dest address to start to clear
 * \param size size of the region to clear
 *
 * Zero \p size bytes starting at \p dest.
 * The address of \p dest MUST be aligned to word boundaries
 *
 * FIXME borrow faster code from some BSD libc or bionic
 */
void
mono_gc_bzero_aligned (void *dest, size_t size)
{
	volatile char *d = (char*)dest;
	size_t tail_bytes, word_bytes;

	g_assert (unaligned_bytes (dest) == 0);

	/* copy all words with memmove */
	word_bytes = (size_t)align_down (size);
	switch (word_bytes) {
	case sizeof (void*) * 1:
		BZERO_WORDS (d, 1);
		break;
	case sizeof (void*) * 2:
		BZERO_WORDS (d, 2);
		break;
	case sizeof (void*) * 3:
		BZERO_WORDS (d, 3);
		break;
	case sizeof (void*) * 4:
		BZERO_WORDS (d, 4);
		break;
	default:
		BZERO_WORDS (d, bytes_to_words (word_bytes));
	}

	tail_bytes = unaligned_bytes (size);
	if (tail_bytes) {
		d += word_bytes;
		do {
			*d++ = 0;
		} while (--tail_bytes);
	}
}

/**
 * mono_gc_bzero_atomic:
 * \param dest address to start to clear
 * \param size size of the region to clear
 *
 * Zero \p size bytes starting at \p dest.
 *
 * Use this to zero memory without word tearing when \p dest is aligned.
 */
void
mono_gc_bzero_atomic (void *dest, size_t size)
{
	if (unaligned_bytes (dest))
		memset (dest, 0, size);
	else
		mono_gc_bzero_aligned (dest, size);
}

#define MEMMOVE_WORDS_UPWARD(dest,src,words) do {	\
		void * volatile *__d = (void* volatile*)(dest);		\
		void **__s = (void**)(src);		\
		int __n = (int)(words);			\
		int __i;				\
		for (__i = 0; __i < __n; ++__i)		\
			__d [__i] = __s [__i];		\
	} while (0)

#define MEMMOVE_WORDS_DOWNWARD(dest,src,words) do {	\
		void * volatile *__d = (void* volatile*)(dest);		\
		void **__s = (void**)(src);		\
		int __n = (int)(words);			\
		int __i;				\
		for (__i = __n - 1; __i >= 0; --__i)	\
			__d [__i] = __s [__i];		\
	} while (0)


/**
 * mono_gc_memmove_aligned:
 * \param dest destination of the move
 * \param src source
 * \param size size of the block to move
 *
 * Move \p size bytes from \p src to \p dest.
 *
 * Use this to copy memory without word tearing when both pointers are aligned
 */
void
mono_gc_memmove_aligned (void *dest, const void *src, size_t size)
{
	g_assert (unaligned_bytes (dest) == 0);
	g_assert (unaligned_bytes (src) == 0);

	/*
	If we're copying less than a word we don't need to worry about word tearing
	so we bailout to memmove early.
	*/
	if (size < sizeof(void*)) {
		memmove (dest, src, size);
		return;
	}

	/*
	 * A bit of explanation on why we align only dest before doing word copies.
	 * Pointers to managed objects must always be stored in word aligned addresses, so
	 * even if dest is misaligned, src will be by the same amount - this ensure proper atomicity of reads.
	 *
	 * We don't need to case when source and destination have different alignments since we only do word stores
	 * using memmove, which must handle it.
	 */
	if (dest > src && ((size_t)((char*)dest - (char*)src) < size)) { /*backward copy*/
			volatile char *p = (char*)dest + size;
			char *s = (char*)src + size;
			char *start = (char*)dest;
			char *align_end = MAX((char*)dest, (char*)align_down (p));
			char *word_start;
			size_t bytes_to_memmove;

			while (p > align_end)
			        *--p = *--s;

			word_start = (char *)align_up (start);
			bytes_to_memmove = p - word_start;
			p -= bytes_to_memmove;
			s -= bytes_to_memmove;
			MEMMOVE_WORDS_DOWNWARD (p, s, bytes_to_words (bytes_to_memmove));
	} else {
		volatile char *d = (char*)dest;
		const char *s = (const char*)src;
		size_t tail_bytes;

		/* copy all words with memmove */
		MEMMOVE_WORDS_UPWARD (d, s, bytes_to_words (align_down (size)));

		tail_bytes = unaligned_bytes (size);
		if (tail_bytes) {
			d += (size_t)align_down (size);
			s += (size_t)align_down (size);
			do {
				*d++ = *s++;
			} while (--tail_bytes);
		}
	}
}

/**
 * mono_gc_memmove_atomic:
 * \param dest destination of the move
 * \param src source
 * \param size size of the block to move
 *
 * Move \p size bytes from \p src to \p dest.
 *
 * Use this to copy memory without word tearing when both pointers are aligned
 */
void
mono_gc_memmove_atomic (void *dest, const void *src, size_t size)
{
	if (unaligned_bytes (_toi (dest) | _toi (src)))
		memmove (dest, src, size);
	else
		mono_gc_memmove_aligned (dest, src, size);
}
