/* libunwind - a platform-independent unwind library
   Copyright (c) 2003, 2005 Hewlett-Packard Development Company, L.P.
        Contributed by David Mosberger-Tang <davidm@hpl.hp.com>

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.  */

// This is minimal implementation functions files required to cross compile
// libunwind on MacOS for UNW_REMOTE_ONLY application.

#include <pthread.h>
#include <signal.h>
#include <stdlib.h>
#include <stdatomic.h>
#include "libunwind_i.h"
#include "compiler.h"

unw_addr_space_t UNW_OBJ(local_addr_space);

unw_accessors_t *
unw_get_accessors (unw_addr_space_t as)
{
  if (!atomic_load(&tdep_init_done))
    tdep_init ();
  return &as->acc;
}

unw_accessors_t * 
unw_get_accessors_int (unw_addr_space_t as)
{
  return unw_get_accessors(as);
}
 
#if defined(TARGET_AMD64) && !defined(HOST_AMD64)
#define X86_64_SCF_NONE 0
#endif

#if defined(TARGET_ARM64) && !defined(HOST_ARM64)
#define AARCH64_SCF_NONE 0
#endif

int
unw_is_signal_frame (unw_cursor_t *cursor)
{
  struct cursor *c = (struct cursor *) cursor;
#ifdef TARGET_AMD64
  return c->sigcontext_format != X86_64_SCF_NONE;
#elif defined(TARGET_ARM64)
  return c->sigcontext_format != AARCH64_SCF_NONE;
#else
  #error Unexpected target
#endif
}

int
UNW_OBJ(handle_signal_frame) (unw_cursor_t *cursor)
{
  return -UNW_EBADFRAME;
}
