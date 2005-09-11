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

#define AO_REQUIRE_CAS
#include "atomic_ops_stack.h"
#include <string.h>	/* for ffs, which is assumed reentrant.	*/
#include <stdlib.h>
#ifdef AO_TRACE_MALLOC
# include <stdio.h>
# include <pthread.h>
#endif

/*
 * We round up each allocation request to the next power of two
 * minus one word.
 * We keep one stack of free objects for each size.  Each object
 * has an initial word (offset -sizeof(AO_t) from the visible pointer)
 * which contains either
 * 	The binary log of the object size in bytes (small objects)
 * 	The object size (a multiple of CHUNK_SIZE) for large objects.
 * The second case only arises if mmap-based allocation is supported.
 * We align the user-visible part of each object on a GRANULARITY
 * byte boundary.  That means that the actual (hidden) start of
 * the object starts a word before this boundary.
 */

#ifndef LOG_MAX_SIZE
# define LOG_MAX_SIZE 16
	/* We assume that 2**LOG_MAX_SIZE is a multiple of page size. */
#endif

#ifndef ALIGNMENT
# define ALIGNMENT 16
	/* Assumed to be at least sizeof(AO_t).		*/
#endif

#define CHUNK_SIZE (1 << LOG_MAX_SIZE)

#ifndef AO_INITIAL_HEAP_SIZE
#  define AO_INITIAL_HEAP_SIZE (2*(LOG_MAX_SIZE+1)*CHUNK_SIZE)
#endif

char AO_initial_heap[AO_INITIAL_HEAP_SIZE];

static volatile AO_t initial_heap_ptr = (AO_t)AO_initial_heap;
static volatile char *initial_heap_lim = AO_initial_heap + AO_INITIAL_HEAP_SIZE;

#if defined(HAVE_MMAP)

#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/mman.h>

static volatile AO_t mmap_enabled = 0;

void
AO_malloc_enable_mmap(void)
{
  AO_store(&mmap_enabled, 1);
}

static char *get_mmaped(size_t sz)
{
  char * result;

  assert(!(sz & (CHUNK_SIZE - 1)));
  if (!mmap_enabled) return 0;
# if defined(MAP_ANONYMOUS)
    result = mmap(0, sz, PROT_READ | PROT_WRITE,
	          MAP_PRIVATE | MAP_ANONYMOUS, 0, 0);
# elif defined(MAP_ANON)
    result = mmap(0, sz, PROT_READ | PROT_WRITE,
	          MAP_PRIVATE | MAP_ANON, -1, 0);
# else
    {
      int zero_fd = open("/dev/zero", O_RDONLY);
      result = mmap(0, sz, PROT_READ | PROT_WRITE,
		    MAP_PRIVATE, zero_fd, 0);
      close(zero_fd);
    }
# endif
  if (result == MAP_FAILED) result = 0;
  return result;
}

/* Allocate an object of size (incl. header) of size > CHUNK_SIZE.	*/
/* sz includes space for an AO_t-sized header.				*/
static char *
AO_malloc_large(size_t sz)
{
 char * result;
 /* The header will force us to waste ALIGNMENT bytes, incl. header.	*/
   sz += ALIGNMENT;
 /* Round to multiple of CHUNK_SIZE.	*/
   sz = (sz + CHUNK_SIZE - 1) & ~(CHUNK_SIZE - 1);
 result = get_mmaped(sz);
 if (result == 0) return 0;
 result += ALIGNMENT;
 ((AO_t *)result)[-1] = (AO_t)sz;
 return result;
}

static void
AO_free_large(char * p)
{
  AO_t sz = ((AO_t *)p)[-1];
  if (munmap(p - ALIGNMENT, (size_t)sz) != 0)
    abort();  /* Programmer error.  Not really async-signal-safe, but ... */
}
  

#else /*  No MMAP */

void
AO_malloc_enable_mmap(void)
{
}

static char *get_mmaped(size_t sz)
{
  return 0;
}

static char *
AO_malloc_large(size_t sz)
{
  return 0;
}

static void
AO_free_large(char * p)
{
  abort();  /* Programmer error.  Not really async-signal-safe, but ... */
}

#endif /* No MMAP */

static char *
get_chunk(void)
{
  char *initial_ptr;
  char *my_chunk_ptr;
  char * my_lim;

retry:
  initial_ptr = (char *)AO_load(&initial_heap_ptr);
  my_chunk_ptr = (char *)(((AO_t)initial_ptr + (ALIGNMENT - 1))
		          & ~(ALIGNMENT - 1));
  if (initial_ptr != my_chunk_ptr)
    {
      /* Align correctly.  If this fails, someone else did it for us.	*/
      AO_compare_and_swap_acquire(&initial_heap_ptr, (AO_t)initial_ptr,
		    		  (AO_t)my_chunk_ptr);
    }
  my_lim = my_chunk_ptr + CHUNK_SIZE;
  if (my_lim <= initial_heap_lim)
    {
      if (!AO_compare_and_swap(&initial_heap_ptr, (AO_t)my_chunk_ptr,
			     			  (AO_t)my_lim))
        goto retry;
      return my_chunk_ptr;
    }
  /* We failed.  The initial heap is used up.	*/
  my_chunk_ptr = get_mmaped(CHUNK_SIZE);
  assert (!((AO_t)my_chunk_ptr & (ALIGNMENT-1)));
  return my_chunk_ptr;
}

/* Object free lists.  Ith entry corresponds to objects	*/
/* of total size 2**i bytes.					*/
AO_stack_t AO_free_list[LOG_MAX_SIZE+1];

/* Chunk free list, linked through first word in chunks.	*/
/* All entries of size CHUNK_SIZE.				*/
AO_stack_t AO_chunk_free_list;

/* Break up the chunk, and add it to the object free list for	*/
/* the given size.  Sz must be a power of two.			*/
/* We have exclusive access to chunk.				*/
static void
add_chunk_as(void * chunk, size_t sz, unsigned log_sz)
{
  char *first = (char *)chunk + ALIGNMENT - sizeof(AO_t);
  char *limit = (char *)chunk + CHUNK_SIZE - sz;
  char *next, *p;

  for (p = first; p <= limit; p = next) {
    next = p + sz;
    AO_stack_push(AO_free_list+log_sz, (AO_t *)p);
  }
}

static int msbs[16] = {0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4};

/* Return the position of the most significant set bit in the	*/
/* argument.							*/
/* We follow the conventions of ffs(), i.e. the least 		*/
/* significant bit is number one.				*/
int msb(size_t s)
{
  int result = 0;
  if ((s & 0xff) != s) {
    /* The following shift often generates warnings on 32-bit arch's	*/
    /* That's OK, because it will never be executed there.		*/
    if (sizeof(size_t) > 4 && (s >> 32) != 0)
      {
	s >>= 32;
	result += 32;
      }
    if ((s >> 16) != 0)
      {
	s >>= 16;
	result += 16;
      }
    if ((s >> 8) != 0)
      {
	s >>= 8;
	result += 8;
      }
  }
  if (s > 15)
    {
      s >>= 4;
      result += 4;
    }
  result += msbs[s];
  return result;
}

void *
AO_malloc(size_t sz)
{
  AO_t *result;
  size_t adj_sz = sz + sizeof(AO_t);
  int log_sz;
  if (sz > CHUNK_SIZE)
    return AO_malloc_large(sz);
  log_sz = msb(adj_sz-1);
  result = AO_stack_pop(AO_free_list+log_sz);
  while (0 == result) {
    void * chunk = get_chunk();
    if (0 == chunk) return 0;
    adj_sz = 1 << log_sz;
    add_chunk_as(chunk, adj_sz, log_sz);
    result = AO_stack_pop(AO_free_list+log_sz);
  }
  *result = log_sz;
# ifdef AO_TRACE_MALLOC
    fprintf(stderr, "%x: AO_malloc(%lu) = %p\n",
		    (int)pthread_self(), (unsigned long)sz, result+1);
# endif
  return result + 1;
}

void
AO_free(void *p)
{
  char *base = (char *)p - sizeof(AO_t);
  int log_sz;

  if (0 == p) return;
  log_sz = *(AO_t *)base;
# ifdef AO_TRACE_MALLOC
    fprintf(stderr, "%x: AO_free(%p sz:%lu)\n", (int)pthread_self(), p,
		    (unsigned long)
		      (log_sz > LOG_MAX_SIZE? log_sz : (1 << log_sz)));
# endif
  if (log_sz > LOG_MAX_SIZE)
    AO_free_large(p);
  else
    AO_stack_push(AO_free_list+log_sz, (AO_t *)base);
}
