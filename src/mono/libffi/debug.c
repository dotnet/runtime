/* -----------------------------------------------------------------------
   debug.c - Copyright (c) 1996 Red Hat, Inc.

   Permission is hereby granted, free of charge, to any person obtaining
   a copy of this software and associated documentation files (the
   ``Software''), to deal in the Software without restriction, including
   without limitation the rights to use, copy, modify, merge, publish,
   distribute, sublicense, and/or sell copies of the Software, and to
   permit persons to whom the Software is furnished to do so, subject to
   the following conditions:

   The above copyright notice and this permission notice shall be included
   in all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
   IN NO EVENT SHALL RED HAT BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */

#include <ffi.h>
#include <ffi_private.h>
#include <stdlib.h>
#include <stdio.h>

/* General debugging routines */

void ffi_stop_here(void)
{
  /* This function is only useful for debugging purposes.
     Place a breakpoint on ffi_stop_here to be notified of 
     significant events. */
}

/* This function should only be called via the FFI_ASSERT() macro */

int ffi_assert(char *file, int line)
{
  fprintf(stderr, "ASSERTION FAILURE: %s line %d\n", file, line);
  ffi_stop_here();
  abort();

  /* This has to return something for the compiler not to complain */
  /*@notreached@*/
  return 0;
}

/* Perform a sanity check on an ffi_type structure */

bool ffi_type_test(ffi_type *a)
{
  /*@-usedef@*/
  FFI_ASSERT(a->type <= FFI_TYPE_LAST);
  FFI_ASSERT(a->type > FFI_TYPE_VOID ? a->size > 0 : 1);
  FFI_ASSERT(a->type > FFI_TYPE_VOID ? a->alignment > 0 : 1);
  FFI_ASSERT(a->type == FFI_TYPE_STRUCT ? a->elements != NULL : 1);
  /*@=usedef@*/

  /* This is a silly thing to return, but it keeps the compiler from
     issuing warnings about "a" not being used in non-debug builds. */
  return (a != NULL);
}
