/*
 * Contributed by Stephen M. Webb <stephen.webb@bregmasoft.ca>
 *
 * This file is part of libunwind, a platform-independent unwind library.
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
#include "libunwind_i.h"

#ifdef UNW_REMOTE_ONLY
bool
unw_address_is_valid(UNUSED unw_word_t addr, UNUSED size_t len)
{
  Debug(1, "remote-only invoked\n");
  return false;
}

#else /* !UNW_REMOTE_ONLY */

static pthread_once_t _unw_address_validator_init_once = PTHREAD_ONCE_INIT;
static sig_atomic_t   _unw_address_validator_initialized = 0;
static int _mem_validate_pipe[2] = {-1, -1};
static bool (*_mem_validate_func) (unw_word_t, size_t);

#pragma weak pthread_once


#ifdef HAVE_PIPE2
static void
_do_pipe2 (int pipefd[2])
{
  int result UNUSED = pipe2 (pipefd, O_CLOEXEC | O_NONBLOCK);
}
#else
static void
_set_pipe_flags (int fd)
{
  int fd_flags = fcntl (fd, F_GETFD, 0);
  int status_flags = fcntl (fd, F_GETFL, 0);
  fd_flags |= FD_CLOEXEC;
  fcntl (fd, F_SETFD, fd_flags);
  status_flags |= O_NONBLOCK;
  fcntl (fd, F_SETFL, status_flags);
}
static void
_do_pipe2 (int pipefd[2])
{
  pipe (pipefd);
  _set_pipe_flags(pipefd[0]);
  _set_pipe_flags(pipefd[1]);
}
#endif
static void
_open_pipe (void)
{
  if (_mem_validate_pipe[0] != -1)
    close (_mem_validate_pipe[0]);
  if (_mem_validate_pipe[1] != -1)
    close (_mem_validate_pipe[1]);
  _do_pipe2 (_mem_validate_pipe);
}
/**
 * Test is a memory address is valid by trying to write from it
 * @param[in]  addr The address to validate
 *
 * @returns true of the memory address is valid (readable), false otherwise.
 *
 * This check works by using the address as a (one-byte) buffer in a
 * write-to-pipe operation.  The write will fail if the memory is not in the
 * process's address space and marked as readable.
 */
static bool
_write_validate (unw_word_t addr)
{
  int ret = -1;
  ssize_t bytes = 0;
  do
    {
      char buf;
      bytes = read (_mem_validate_pipe[0], &buf, 1);
    }
  while ( errno == EINTR );
  if (!(bytes > 0 || errno == EAGAIN || errno == EWOULDBLOCK))
    {
      // re-open closed pipe
      _open_pipe ();
    }
  do
    {
#ifdef HAVE_SYS_SYSCALL_H
      /* use syscall insteadof write() so that ASAN does not complain */
      ret = syscall (SYS_write, _mem_validate_pipe[1], addr, 1);
#else
      ret = write (_mem_validate_pipe[1], (void *)addr, 1);
#endif
    }
  while ( errno == EINTR );
  return ret > 0;
}


static bool
_msync_validate (unw_word_t addr, size_t len)
{
  if (msync ( (void *)unw_page_start (addr), len, MS_ASYNC) != 0)
    {
      return false;
    }

  return _write_validate (addr);
}


#ifdef HAVE_MINCORE
static bool
_mincore_validate (unw_word_t addr, size_t len)
{
  unsigned char mvec[2]; /* Unaligned access may cross page boundary */

  /* mincore could fail with EAGAIN but we conservatively return false
     instead of looping. */
  if (mincore ((void *)unw_page_start (addr), len, mvec) != 0)
    {
      return false;
    }

  return _write_validate (addr);
}
#endif


static void
_unw_address_validator_init(void)
{
  _open_pipe ();

  /* Work out dynamically what memory validation function to use. */
#ifdef HAVE_MINCORE
  unsigned char present = 1;
  size_t len = unw_page_size;
  unw_word_t addr = unw_page_start((unw_word_t)&present);
  unsigned char mvec[1];
  int ret;
  do
    {
      ret = mincore ((void*)addr, len, mvec);
    }
  while (ret == -1 && errno == EAGAIN);
  if (ret == 0)
    {
      Debug(1, "using mincore to validate memory\n");
      _mem_validate_func = _mincore_validate;
    }
  else
#endif
    {
      Debug(1, "using msync to validate memory\n");
      _mem_validate_func = _msync_validate;
    }
  _unw_address_validator_initialized = ~0;
}

/* Cache of already validated addresses */
enum { NLGA = 4 };

#if defined(HAVE___CACHE_PER_THREAD) && HAVE___CACHE_PER_THREAD
// thread-local variant
static _Thread_local unw_word_t last_good_addr[NLGA];
static _Thread_local int lga_victim;
static bool
_is_cached_valid_mem(unw_word_t addr)
{
  addr = unw_page_start (addr);
  int i;
  for (i = 0; i < NLGA; i++)
    {
      if (addr == last_good_addr[i])
        return true;
    }
  return false;
}
static void
_cache_valid_mem(unw_word_t addr)
{
  addr = unw_page_start (addr);
  int i, victim;
  victim = lga_victim;
  for (i = 0; i < NLGA; i++)
    {
      if (last_good_addr[victim] == 0)
        {
          last_good_addr[victim] = addr;
          return;
        }
      victim = (victim + 1) % NLGA;
    }
  /* All slots full. Evict the victim. */
  last_good_addr[victim] = addr;
  victim = (victim + 1) % NLGA;
  lga_victim = victim;
}
#else
// global, thread safe variant
static _Atomic unw_word_t last_good_addr[NLGA];
static _Atomic int lga_victim;
static bool
_is_cached_valid_mem(unw_word_t addr)
{
  int i;
  addr = unw_page_start (addr);
  for (i = 0; i < NLGA; i++)
    {
      if (addr == atomic_load(&last_good_addr[i]))
        return true;
    }
  return false;
}
/**
 * Adds a known-valid page address to the cache.
 *
 * This implementation is racy as all get-out but the worst case is that cached
 * address get lost, forcing extra unnecessary validation checks.  All of the
 * atomic operatrions don't matter because of TOCTOU races.
 */
static void
_cache_valid_mem(unw_word_t addr)
{
  int i, victim;
  victim = atomic_load(&lga_victim);
  unw_word_t zero = 0;
  addr = unw_page_start (addr);
  for (i = 0; i < NLGA; i++)
    {
      if (atomic_compare_exchange_strong(&last_good_addr[victim], &zero, addr))
        {
          return;
        }
      victim = (victim + 1) % NLGA;
    }
  /* All slots full. Evict the victim. */
  atomic_store(&last_good_addr[victim], addr);
  victim = (victim + 1) % NLGA;
  atomic_store(&lga_victim, victim);
}
#endif
/**
 * Validate an address is readable
 * @param[in]  addr The (starting) address of the memory to validate
 * @param[in]  len  The size of the memory to validate in bytes
 *
 * Validates the memory at address @p addr is readable. Since the granularity of
 * memory readability is the page, only one byte needs to be validated per page
 * for each page starting at @p addr and encompassing @p len bytes.
 *
 * @returns true if the memory is readable, false otherwise.
 */
bool
unw_address_is_valid(unw_word_t addr, size_t len)
{
  if (len == 0)
    return true;
  if (unw_page_start (addr) == 0)
    return false;

  /*
   * First time through initialize everything: once case if linked with pthreads
   * and another when pthreads are not linked (which assumes the single-threaded
   * case).
   *
   * There is a potential race condition in the second case if multiple signals
   * are raised at exactly the same time but the worst case is that several
   * unnecessary validations get done.
   */
  if (likely (pthread_once != NULL))
    {
      pthread_once (&_unw_address_validator_init_once, _unw_address_validator_init);
    }
  else if (unlikely (_unw_address_validator_initialized == 0))
    {
      _unw_address_validator_init();
    }

  unw_word_t lastbyte = addr + (len - 1); // highest addressed byte of data to access
  while (1)
    {
      if (!_is_cached_valid_mem(addr))
        {
          if (!_mem_validate_func (addr, len))
            {
              Debug(1, "returning false\n");
              return false;
            }
          _cache_valid_mem(addr);
        }
      // If we're still on the same page, we're done.
      size_t stride = len-1 < (size_t) unw_page_size ? len-1 : (size_t) unw_page_size;
      len -= stride;
      addr += stride;
      if (unw_page_start (addr) == unw_page_start (lastbyte))
        break;
    }
  return true;
}
#endif /* !UNW_REMOTE_ONLY */
