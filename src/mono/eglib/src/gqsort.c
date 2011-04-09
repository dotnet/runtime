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

#define SWAPTYPE(TYPE, a, b) {              \
	long __n = size / sizeof (TYPE);    \
	register TYPE *__a = (TYPE *) (a);  \
	register TYPE *__b = (TYPE *) (b);  \
	register TYPE t;                    \
	                                    \
	do {                                \
		t = *__a;                   \
		*__a++ = *__b;              \
		*__b++ = t;                 \
	} while (--__n > 0);                \
}

#define SWAPBYTE(a, b) SWAPTYPE(char, (a), (b))
#define SWAPLONG(a, b) SWAPTYPE(long, (a), (b))
#define SWAP(a, b) if (swaplong) SWAPLONG((a), (b)) else SWAPBYTE((a), (b))

/* check if we can swap by longs rather than bytes by making sure that
 * memory is properly aligned and that the element size is a multiple
 * of sizeof (long) */
#define SWAP_INIT() swaplong = (((char *) base) - ((char *) 0)) % sizeof (long) == 0 && (size % sizeof (long)) == 0

void
g_qsort_with_data (gpointer base, size_t nmemb, size_t size, GCompareDataFunc compare, gpointer user_data)
{
	QSortStack stack[STACK_SIZE], *sp;
	register char *i, *k, *mid;
	size_t n, n1, n2;
	char *lo, *hi;
	int swaplong;
	
	if (nmemb <= 1)
		return;
	
	SWAP_INIT ();
	
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
					SWAP (k - size, k);
			
			continue;
		}
		
		/* calculate the middle element */
		mid = lo + (n / 2) * size;
		
		/* once we re-order the lo, mid, and hi elements to be in
		 * ascending order, we'll use mid as our pivot. */
		if (compare (mid, lo, user_data) < 0) {
			SWAP (mid, lo);
		}
		
		if (compare (hi, mid, user_data) < 0) {
			SWAP (mid, hi);
			if (compare (mid, lo, user_data) < 0) {
				SWAP (mid, lo);
			}
		}
		
		/* since we've already guaranteed that lo <= mid and mid <= hi,
		 * we can skip comparing them again */
		i = lo + size;
		k = hi - size;
		
		do {
			/* find the first element with a value > pivot value */
			while (i < k && compare (i, mid, user_data) <= 0)
				i += size;
			
			/* find the last element with a value <= pivot value */
			while (k >= i && compare (mid, k, user_data) < 0)
				k -= size;
			
			if (k <= i)
				break;
			
			SWAP (i, k);
			
			/* make sure we keep track of our pivot element */
			if (mid == i) {
				mid = k;
			} else if (mid == k) {
				mid = i;
			}
			
			i += size;
			k -= size;
		} while (1);
		
		if (k != mid) {
			/* swap the pivot with the last element in the first partition */
			SWAP (mid, k);
		}
		
		/* calculate segment sizes */
		n2 = (hi - k) / size;
		n1 = (k - lo) / size;
		
		/* push our partitions onto the stack, largest first
		 * (to make sure we don't run out of stack space) */
		if (n2 > n1) {
			if (n2 > 1) QSORT_PUSH (sp, k + size, n2);
			if (n1 > 1) QSORT_PUSH (sp, lo, n1);
		} else {
			if (n1 > 1) QSORT_PUSH (sp, lo, n1);
			if (n2 > 1) QSORT_PUSH (sp, k + size, n2);
		}
	} while (sp > stack);
}
