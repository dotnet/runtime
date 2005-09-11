/*  
 * Copyright (c) 2003-2005 Hewlett-Packard Development Company, L.P.
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

#include "test_atomic_include.h"

#ifdef AO_USE_PTHREAD_DEFS
# define NITERS 100000
#else
# define NITERS 10000000
#endif

#if defined(AO_HAVE_fetch_and_add1) && defined(AO_HAVE_fetch_and_sub1)

AO_t counter = 0;

void * add1sub1_thr(void * id)
{
  int me = (int)(long)id;

  int i;

  for (i = 0; i < NITERS; ++i)
    if (me & 1)
      AO_fetch_and_sub1(&counter);
    else
      AO_fetch_and_add1(&counter);

  return 0;
}

int add1sub1_test(void)
{
  return counter == 0;
}

#endif /* defined(AO_HAVE_fetch_and_add1) && defined(AO_HAVE_fetch_and_sub1) */

#if defined(AO_HAVE_store_release_write) && defined(AO_HAVE_load_acquire_read)

/* Invariant: counter1 >= counter2 */
AO_t counter1 = 0;
AO_t counter2 = 0;

void * acqrel_thr(void *id)
{
  int me = (int)(long)id;

  int i;

  for (i = 0; i < NITERS; ++i)
    if (me & 1)
      {
        AO_t my_counter1;
	if (me != 1)
	  fprintf(stderr, "acqrel test: too many threads\n");
	my_counter1 = AO_load(&counter1);
	AO_store(&counter1, my_counter1 + 1);
	AO_store_release_write(&counter2, my_counter1 + 1);
      }
    else
      {
	AO_t my_counter1a, my_counter2a;
	AO_t my_counter1b, my_counter2b;

	my_counter2a = AO_load_acquire_read(&counter2);
	my_counter1a = AO_load(&counter1);
	/* Redo this, to make sure that the second load of counter1	*/
	/* is not viewed as a common subexpression.			*/
	my_counter2b = AO_load_acquire_read(&counter2);
	my_counter1b = AO_load(&counter1);
	if (my_counter1a < my_counter2a)
	  {
	    fprintf(stderr, "Saw release store out of order: %lu < %lu\n",
		    (unsigned long)my_counter1a, (unsigned long)my_counter2a);
	    abort();
	  }
	if (my_counter1b < my_counter2b)
	  {
	    fprintf(stderr,
		    "Saw release store out of order (bad CSE?): %lu < %lu\n",
		    (unsigned long)my_counter1b, (unsigned long)my_counter2b);
	    abort();
	  }
      }

  return 0;
}

int acqrel_test(void)
{
  return counter1 == NITERS && counter2 == NITERS;
}

#endif /* AO_HAVE_store_release_write && AO_HAVE_load_acquire_read */

#if defined(AO_HAVE_test_and_set_acquire)

AO_TS_T lock = AO_TS_INITIALIZER;

unsigned long locked_counter;
volatile unsigned long junk = 13;

void * test_and_set_thr(void * id)
{
  unsigned long i;

  for (i = 0; i < NITERS/10; ++i)
    {
      while (AO_test_and_set_acquire(&lock) != AO_TS_CLEAR);
      ++locked_counter;
      if (locked_counter != 1)
        {
          fprintf(stderr, "Test and set failure 1, counter = %ld\n",
    		      locked_counter);
          abort();
        }
      locked_counter *= 2;
      locked_counter -= 1;
      locked_counter *= 5;
      locked_counter -= 4;
      if (locked_counter != 1)
        {
          fprintf(stderr, "Test and set failure 2, counter = %ld\n",
    		      locked_counter);
          abort();
        }
      --locked_counter;
      AO_CLEAR(&lock);
      /* Spend a bit of time outside the lock. */
        junk *= 17;
        junk *= 17;
    }
  return 0;
}

int test_and_set_test(void)
{
  return locked_counter == 0;
}

#endif /* defined(AO_HAVE_test_and_set_acquire) */

int main()
{
  test_atomic();
  test_atomic_acquire();
  test_atomic_release();
  test_atomic_read();
  test_atomic_write();
  test_atomic_full();
  test_atomic_release_write();
  test_atomic_acquire_read();
# if defined(AO_HAVE_fetch_and_add1) && defined(AO_HAVE_fetch_and_sub1)
    run_parallel(4, add1sub1_thr, add1sub1_test, "add1/sub1");
# endif
# if defined(AO_HAVE_store_release_write) && defined(AO_HAVE_load_acquire_read)
    run_parallel(3, acqrel_thr, acqrel_test,
		 "store_release_write/load_acquire_read");
# endif
# if defined(AO_HAVE_test_and_set_acquire)
    run_parallel(5, test_and_set_thr, test_and_set_test,
		 "test_and_set");
# endif
  return 0;
}
