/*
 * QuickSort
 *
 * Author: Jeffrey Stedfast <fejj@novell.com>
 *
 * (C) 2011 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
#include <stdlib.h>
#include <glib.h>

/* Any segment <= this threshold will be sorted using insertion
 * sort. OpenBSD seems to use a value of 7 so we'll go with that for
 * now... */
#define MAX_THRESHOLD 7

#define STACK_SIZE (8 * sizeof (size_t))

typedef struct _QSortStack {
	char *array;
	size_t count;
} QSortStack;

#define QSORT_PUSH(sp, a, c) (sp->array = a, sp->count = c, sp++)
#define QSORT_POP(sp, a, c) (sp--, a = sp->array, c = sp->count)

static inline void
g_qsort_swap (char *a, char *b, size_t n)
{
	register char *an = a + n;
	register char tmp;
	
	do {
		tmp = *a;
		*a++ = *b;
		*b++ = tmp;
	} while (a < an);
}

static inline char *
g_qsort_median (char *a, char *b, char *c, GCompareDataFunc compare, gpointer user_data)
{
	if (compare (a, b, user_data) < 0) {
		/* a < b < c */
		if (compare (b, c, user_data) < 0)
			return b;
		
		/* a < c < b */
		if (compare (a, c, user_data) < 0)
			return c;
		
		/* c < a < b */
		return a;
	} else {
		/* b < a < c */
		if (compare (a, c, user_data) < 0)
			return a;
		
		/* b < c < a */
		if (compare (b, c, user_data) < 0)
			return c;
		
		/* c < b < a */
		return b;
	}
}

void
g_qsort_with_data (gpointer base, size_t nmemb, size_t size, GCompareDataFunc compare, gpointer user_data)
{
	QSortStack stack[STACK_SIZE], *sp;
	register char *i, *k, *pivot;
	char *mid, *lo, *hi;
	size_t n, n1, n2;
	
	/* initialize our stack */
	sp = stack;
	QSORT_PUSH (sp, base, nmemb);
	
	do {
		QSORT_POP (sp, lo, n);
		
		hi = lo + (n - 1) * size;
		
		if (n < MAX_THRESHOLD) {
			/* switch to insertion sort */
			for (i = lo + size; i <= hi; i += size)
				for (k = i; k > lo && compare (k - size, k, user_data) > 0; k -= size)
					g_qsort_swap (k - size, k, size);
			
			continue;
		}
		
		/* calculate the middle element */
		mid = lo + (n / 2) * size;
		
		/* determine which element contains the median value */
		pivot = g_qsort_median (lo, mid, hi, compare, user_data);
		
		if (pivot != lo) {
			/* swap pivot value into first element (so the location stays constant) */
			g_qsort_swap (lo, pivot, size);
			pivot = lo;
		}
		
		i = lo + size;
		k = hi;
		
		do {
			/* find the first element with a value > pivot value */
			while (i < k && compare (i, pivot, user_data) <= 0)
				i += size;
			
			/* find the last element with a value <= pivot value */
			while (k >= i && compare (k, pivot, user_data) > 0)
				k -= size;
			
			if (k <= i)
				break;
			
			g_qsort_swap (i, k, size);
		} while (1);
		
		if (k != pivot) {
			/* swap the pivot with the last element in the first partition */
			g_qsort_swap (pivot, k, size);
		}
		
		/* calculate segment sizes */
		n2 = (hi - k) / size;
		n1 = (k - lo) / size;
		
		if (n1 == 1 || n2 == 1) {
			/* pathological case detected, switch to insertion sort */
			for (i = lo + size; i <= hi; i += size)
				for (k = i; k > lo && compare (k - size, k, user_data) > 0; k -= size)
					g_qsort_swap (k - size, k, size);
		} else {
			/* push each segment onto the stack */
			QSORT_PUSH (sp, k + size, n2);
			QSORT_PUSH (sp, lo, n1);
		}
	} while (sp > stack);
}
