/*  
 * Copyright (c) 2005 Hewlett-Packard Development Company, L.P.
 * Original Author: Hans Boehm
 *
 * This file may be redistributed and/or modified under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation; either version 2, or (at your option) any later version.
 * 
 * It is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE.  See the GNU General Public License in the
 * file doc/COPYING for more details.
 */

#if defined(HAVE_CONFIG_H)
# include "config.h"
#endif

#include "run_parallel.inc"

#include <stdlib.h>
#include <stdio.h>
#include "atomic_ops_malloc.h"
#define MAX_NTHREADS 100
#define N_REVERSALS 1000 /* must be even */
#define LENGTH 1000

#ifdef USE_STANDARD_MALLOC
# define AO_malloc(n) malloc(n)
# define AO_free(p) free(p)
# define AO_malloc_enable_mmap() 
#endif

typedef struct list_node {
	struct list_node *next;
	int data;
} ln;

ln *cons(int d, ln *tail)
{
  static size_t extra = 0;
  size_t my_extra = extra;
  ln *result;
  int * extras;
  int i;

  if (my_extra > 100) 
    extra = my_extra = 0;
  else
    ++extra;
  result = AO_malloc(sizeof(ln) + sizeof(int)*my_extra);
  if (result == 0)
    {
      fprintf(stderr, "Out of memory\n");
      	/* Normal for more than about 10 threads without mmap? */
      abort();
    }

  result -> data = d;
  result -> next = tail;
  extras = (int *)(result+1);
  for (i = 0; i < my_extra; ++i) extras[i] = 42;
  return result;
}

void print_list(ln *l)
{
  ln *p;

  for (p = l; p != 0; p = p -> next)
    {
      fprintf(stderr, "%d, ", p -> data);
    }
  fprintf(stderr, "\n");
}

/* Check that l contains numbers from m to n inclusive in ascending order */
void check_list(ln *l, int m, int n)
{
  ln *p;
  int i;

  for (p = l, i = m; p != 0; p = p -> next, ++i)
    {
      if (i != p -> data)
	{
	  fprintf(stderr, "Found %d, expected %d\n", p -> data, i);
	  abort();
	}
    }
}

/* Create a list of integers from m to n */
ln *
make_list(int m, int n)
{
  if (m > n) return 0;
  return cons(m, make_list(m+1, n));
}

/* Reverse list x, and concatenate it to y, deallocating no longer needed */
/* nodes in x.								  */
ln *
reverse(ln *x, ln *y)
{
  ln * result;

  if (x == 0) return y;
  result = reverse(x -> next, cons(x -> data, y));
  AO_free(x);
  return result;
}

int dummy_test(void) { return 1; }

#define LARGE 200000

void * run_one_test(void * arg) {
  ln * x = make_list(1, LENGTH);
  int i;
  char *p = AO_malloc(LARGE);
  char *q;

  if (0 == p) {
    fprintf(stderr, "AO_malloc(%d) failed: This is normal without mmap\n",
	    LARGE);
    AO_free(p);
  } else {
    p[0] = p[LARGE/2] = p[LARGE-1] = 'a';
    q = AO_malloc(LARGE);
    q[0] = q[LARGE/2] = q[LARGE-1] = 'b';
    if (p[0] != 'a' || p[LARGE/2] != 'a' || p[LARGE-1] != 'a') {
      fprintf(stderr, "First large allocation smashed\n");
      abort();
    }
    AO_free(p);
    if (q[0] != 'b' || q[LARGE/2] != 'b' || q[LARGE-1] != 'b') {
      fprintf(stderr, "Second large allocation smashed\n");
      abort();
    }
    AO_free(q);
  }
# if 0 /* enable for debugging */
    x = reverse(x, 0);
    print_list(x);
    x = reverse(x, 0);
    print_list(x);
# endif
  for (i = 0; i < N_REVERSALS; ++i) {
    x = reverse(x, 0);
  }
  check_list(x, 1, LENGTH);
  return 0;
}

int main(int argc, char **argv) {
    int nthreads;
    int exper_n;

    if (1 == argc) {
#     if !defined(HAVE_MMAP)
	nthreads = 3;
#     else
        nthreads = 10;
#     endif
    } else if (2 == argc) {
      nthreads = atoi(argv[1]);
      if (nthreads < 1 || nthreads > MAX_NTHREADS) {
    	fprintf(stderr, "Invalid # of threads argument\n");
    	exit(1);
      }
    } else {
      fprintf(stderr, "Usage: %s [# of threads]\n", argv[0]);
      exit(1);
    }
    printf("Performing %d reversals of %d element lists in %d threads\n",
	   N_REVERSALS, LENGTH, nthreads);
    AO_malloc_enable_mmap();
    run_parallel(nthreads, run_one_test, dummy_test, "AO_malloc/AO_free");
    return 0;
}

