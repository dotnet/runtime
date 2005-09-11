/*
 * Copyright (c) 2003-2005 Hewlett-Packard Development Company, L.P.
 *
 * This file is covered by the GNU general public license, version 2.
 * see doc/COPYING for details.
 */

#if defined(_MSC_VER) || \
    defined(_WIN32) && !defined(__CYGWIN32__) && !defined(__CYGWIN__) || \
    defined(_WIN32_WINCE)
#  define USE_WINTHREADS
#else
#  define USE_PTHREADS
#endif

#include <stdlib.h>
#include <stdio.h>

#ifdef USE_PTHREADS
# include <pthread.h>
#endif

#ifdef USE_WINTHREADS
# include <windows.h>
#endif

#include "atomic_ops.h"

typedef void * (* thr_func)(void *);

typedef int (* test_func)(void);	/* Returns != 0 on success */

#ifdef USE_PTHREADS
void * run_parallel(int nthreads, thr_func f1, test_func t, char *name)
{
  pthread_attr_t attr;
  pthread_t thr[100];
  int i;
  int code;

  fprintf(stderr, "Testing %s\n", name);
  if (nthreads > 100) 
    {
      fprintf(stderr, "run_parallel: requested too many threads\n");
      abort();
    }

# ifdef _HPUX_SOURCE
   /* Default stack size is too small, especially with the 64 bit ABI */
   /* Increase it.                                                    */
    if (pthread_default_stacksize_np(1024*1024, 0) != 0) {
      fprintf(stderr, "pthread_default_stacksize_np failed. "
      		      "OK after first call.\n");
    }
# endif

  pthread_attr_init(&attr);

  for (i = 0; i < nthreads; ++i)
    {
      if ((code = pthread_create(thr + i, &attr, f1, (void *)(long)i)) != 0)
	{
	  perror("Thread creation failed");
	  fprintf(stderr, "Pthread_create returned %d, thread %d\n", code, i);
	  abort();
        }
    }
  for (i = 0; i < nthreads; ++i)
    {
      if ((code = pthread_join(thr[i], NULL)) != 0)
	{
	  perror("Thread join failed");
	  fprintf(stderr, "Pthread_join returned %d, thread %d\n", code, i);
	  abort();
        }
    }
  if (t())
    {
      fprintf(stderr, "Succeeded\n");
    }
  else
    {
      fprintf(stderr, "Failed\n");
      abort();
    }
  return 0;
}
#endif /* USE_PTHREADS */

#ifdef USE_WINTHREADS

struct tramp_args {
  thr_func fn;
  long arg;
};

DWORD WINAPI tramp(LPVOID param)
{
  struct tramp_args *args = (struct tramp_args *)param;

  return (DWORD)(args -> fn)((LPVOID)(args -> arg));
}

void * run_parallel(int nthreads, thr_func f1, test_func t, char *name)
{
  HANDLE thr[100];
  struct tramp_args args[100];
  int i;
  DWORD code;

  fprintf(stderr, "Testing %s\n", name);
  if (nthreads > 100) 
    {
      fprintf(stderr, "run_parallel: requested too many threads\n");
      abort();
    }

  for (i = 0; i < nthreads; ++i)
    {
      args[i].fn = f1;
      args[i].arg = i;
      if ((thr[i] = CreateThread(NULL, 0, tramp, (LPVOID)(args+i), 0, NULL))
	  == NULL)
	{
	  perror("Thread creation failed");
	  fprintf(stderr, "CreateThread failed with %d, thread %d\n",
			  GetLastError(), i);
	  abort();
        }
    }
  for (i = 0; i < nthreads; ++i)
    {
      if ((code = WaitForSingleObject(thr[i], INFINITE)) != WAIT_OBJECT_0)
	{
	  perror("Thread join failed");
	  fprintf(stderr, "WaitForSingleObject returned %d, thread %d\n",
			  code, i);
	  abort();
        }
    }
  if (t())
    {
      fprintf(stderr, "Succeeded\n");
    }
  else
    {
      fprintf(stderr, "Failed\n");
      abort();
    }
  return 0;
}
#endif /* USE_WINTHREADS */

