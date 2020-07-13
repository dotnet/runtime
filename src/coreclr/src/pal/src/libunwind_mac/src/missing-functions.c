// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
 
int
unw_is_signal_frame (unw_cursor_t *cursor)
{
  struct cursor *c = (struct cursor *) cursor;
  return c->sigcontext_format != X86_64_SCF_NONE;
}

int
UNW_OBJ(handle_signal_frame) (unw_cursor_t *cursor)
{
  return -UNW_EBADFRAME;
}
