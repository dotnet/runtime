// -*- C++ -*-
//===----------------------------------------------------------------------===//
//
// Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
// See https://llvm.org/LICENSE.txt for license information.
// SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
//
//===----------------------------------------------------------------------===//

#ifndef _LIBCPP_CSTDARG_COMPAT
#define _LIBCPP_CSTDARG_COMPAT

/*
    cstdarg synopsis

Macros:

    type va_arg(va_list ap, type);
    void va_copy(va_list dest, va_list src);  // C99
    void va_end(va_list ap);
    void va_start(va_list ap, parmN);

namespace std
{

Types:

    va_list

}  // std

*/

#include "stdarg-compat.h"

#if !defined(_LIBCPP_HAS_NO_PRAGMA_SYSTEM_HEADER)
#  pragma GCC system_header
#endif

#endif
