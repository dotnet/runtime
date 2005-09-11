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

#include <pthread.h>
#include <stdlib.h>
#include <stdio.h>
#include "atomic_ops.h"
#include "atomic_ops_stack.h"
#define MAX_NTHREADS 100

#ifndef NO_TIMES
#include <time.h>
#include <sys/time.h>
/* Need 64-bit long long support */
long long
get_msecs(void)
{
  struct timeval tv;

  gettimeofday(&tv, 0);
  return (long long)tv.tv_sec * 1000 + tv.tv_usec/1000;
}
#else
# define get_msecs() 0
#endif

typedef struct le {
    AO_t next;
    int data;
} list_element;

AO_stack_t the_list = AO_STACK_INITIALIZER;

void add_elements(int n)
{
  list_element * le;
  if (n == 0) return;
  add_elements(n-1);
  le = malloc(sizeof(list_element));
  le -> data = n;
  AO_stack_push(&the_list, (AO_t *)le);
}

void print_list()
{
  list_element *p;

  for (p = (list_element *)AO_REAL_HEAD_PTR(the_list);
       p != 0;
       p = (list_element *)AO_REAL_NEXT_PTR(p -> next))
    printf("%d\n", p -> data);
}

static char marks[MAX_NTHREADS * MAX_NTHREADS];

void check_list(int n)
{
  list_element *p;
  int i;

  for (i = 1; i <= n; ++i) marks[i] = 0;
  for (p = (list_element *)AO_REAL_HEAD_PTR(the_list);
       p != 0;
       p = (list_element *)AO_REAL_NEXT_PTR(p -> next))
    {
      if (p -> data > n || p -> data <= 0)
        fprintf(stderr, "Found erroneous list element %d\n", p -> data);
      if (marks[p -> data] != 0)
        fprintf(stderr, "Found duplicate list element %d\n", p -> data);
      marks[p -> data] = 1;
    }
  for (i = 1; i <= n; ++i)
    if (marks[i] != 1)
      fprintf(stderr, "Missing list element %d\n", i);
}
     
volatile AO_t ops_performed = 0;

#define LIMIT 1000000
	/* Total number of push/pop ops in all threads per test. */

#ifdef AO_HAVE_fetch_and_add
# define fetch_and_add(addr, val) AO_fetch_and_add(addr, val)
#else
  /* Fake it.  This is really quite unacceptable for timing	*/
  /* purposes.  But as a correctness test, it should be OK.	*/
  AO_INLINE AO_t fetch_and_add(volatile AO_t * addr, AO_t val)
  {
    AO_t result = AO_load(addr);
    AO_store(addr, result + val);
    return result;
  }
#endif

void * run_one_test(void * arg)
{
  list_element * t[MAX_NTHREADS + 1];
  list_element * aux; 
  long index = (long)arg;
  int i;
  int j = 0;

# ifdef VERBOSE
    printf("starting thread %d\n", index);
# endif
  while (fetch_and_add(&ops_performed, index + 1) + index + 1 < LIMIT)
    {
      for (i = 0; i < index + 1; ++i)
        {
          t[i] = (list_element *)AO_stack_pop(&the_list);
          if (0 == t[i])
	    {
              fprintf(stderr, "FAILED\n");
              abort();
            }
        }
      for (i = 0; i < index + 1; ++i)
        {
          AO_stack_push(&the_list, (AO_t *)t[i]);
        }
      j += (index + 1);
    }
# ifdef VERBOSE
    printf("finished thread %d: %d total ops\n", index, j);
# endif
  return 0;
}

#define N_EXPERIMENTS 1

unsigned long times[MAX_NTHREADS + 1][N_EXPERIMENTS];

int main(int argc, char **argv)
{
  int nthreads;
  int max_nthreads;
  int exper_n;

  if (1 == argc)
    max_nthreads = 4;
  else if (2 == argc)
    {
      max_nthreads = atoi(argv[1]);
      if (max_nthreads < 1 || max_nthreads > MAX_NTHREADS)
        {
    	  fprintf(stderr, "Invalid max # of threads argument\n");
    	  exit(1);
        }
    }
  else
    {
      fprintf(stderr, "Usage: %s [max # of threads]\n");
      exit(1);
    }
  for (exper_n = 0; exper_n < N_EXPERIMENTS; ++ exper_n)
    for (nthreads = 1; nthreads <= max_nthreads; ++nthreads)
      {
        int i;
        pthread_t thread[MAX_NTHREADS];
        int list_length = nthreads*(nthreads+1)/2;
        long long start_time;
  
        add_elements(list_length);
  #     ifdef VERBOSE
          printf("Initial list (nthreads = %d):\n", nthreads);
          print_list();
  #     endif
        ops_performed = 0;
        start_time = get_msecs();
        for (i = 1; i < nthreads; ++i) {
      	int code;
  
          if ((code = pthread_create(thread+i, 0, run_one_test,
  	    (void *)(long)i)) != 0) {
      	      fprintf(stderr, "Thread creation failed %u\n", code);
            exit(1);
          }
        }
        /* We use the main thread to run one test.  This allows gprof	*/
        /* profiling to work, for example.				*/
          run_one_test(0);
        for (i = 1; i < nthreads; ++i) {
      	  int code;
          if ((code = pthread_join(thread[i], 0)) != 0) {
      	    fprintf(stderr, "Thread join failed %u\n", code);
          }
        }
        times[nthreads][exper_n] = (unsigned long)(get_msecs() - start_time);
  #     ifdef VERBOSE
          printf("%d %lu\n", nthreads,
			     (unsigned long)(get_msecs() - start_time));
          printf("final list (should be reordered initial list):\n");
          print_list();
  #     endif
        check_list(list_length);
        while ((list_element *)AO_stack_pop(&the_list));
      }
# ifndef NO_TIMES
    for (nthreads = 1; nthreads <= max_nthreads; ++nthreads)
      {
        unsigned long sum = 0;

        printf("About %d pushes + %d pops in %d threads:",
		LIMIT, LIMIT, nthreads);
        for (exper_n = 0; exper_n < N_EXPERIMENTS; ++exper_n)
	  {
#           if defined(VERBOSE)
	      printf("[%lu] ", times[nthreads][exper_n]);
#	    endif
	    sum += times[nthreads][exper_n];
          }
        printf(" %lu msecs\n", (sum + N_EXPERIMENTS/2)/N_EXPERIMENTS);
      }
# endif /* NO_TIMES */
  return 0;
}

