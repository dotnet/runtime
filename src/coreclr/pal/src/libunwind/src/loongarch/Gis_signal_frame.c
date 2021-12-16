/* libunwind - a platform-independent unwind library
   Copyright (C) 2015 Imagination Technologies Limited
   Copyright (C) 2008 CodeSourcery

This file is part of libunwind.

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

#include "unwind_i.h"
#include <stdio.h>

int
unw_is_signal_frame (unw_cursor_t *cursor)
{
  struct cursor *c = (struct cursor *) cursor;
  unw_word_t w0, w1, ip;
  unw_addr_space_t as;
  unw_accessors_t *a;
  void *arg;
  int ret;

  ip = c->dwarf.ip;

  as = c->dwarf.as;
  a = unw_get_accessors (as);
  arg = c->dwarf.as_arg;

  /* syscall */
  /* FIXME for LOONGARCH: should confirm--- why is "ip+4" !!! */
  //if ((ret = (*a->access_mem) (as, ip + 4, &w1, 0, arg)) < 0)
  if ((ret = (*a->access_mem) (as, ip - 4, &w1, 0, arg)) < 0)
    return 0;
  if ((w1 & 0xffffffff) != 0x002b0000)
    return 0;

  /* addi.w a7,r0,??? */
  //if ((ret = (*a->access_mem) (as, ip, &w0, 0, arg)) < 0)
  if ((ret = (*a->access_mem) (as, ip - 8, &w0, 0, arg)) < 0)
    return 0;

  switch (c->dwarf.as->abi)
    {
    case UNW_LOONGARCH_ABI_LP32:
      /* FIXME for LOONGARCH32! not supported!!! */
      //switch (w0 & 0xffffffff)
      //  {
      //  case 0x0:
      //    return 1;
      //  default:
          return 0;
      //  }
    case UNW_LOONGARCH_ABI_LP64:
      switch (w0 & 0xffffffff)
        {//addi.w a7,139  should confirm further!
        case 0x02822c0b:
          return 1;
        default:
          return 0;
        }
    default:
      return 0;
    }
}
