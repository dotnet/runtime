// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       tuklib_open_stdxxx.h
/// \brief      Make sure that file descriptors 0, 1, and 2 are open
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef TUKLIB_OPEN_STDXXX_H
#define TUKLIB_OPEN_STDXXX_H

#include "tuklib_common.h"
TUKLIB_DECLS_BEGIN

#define tuklib_open_stdxx TUKLIB_SYMBOL(tuklib_open_stdxxx)
extern void tuklib_open_stdxxx(int err_status);

TUKLIB_DECLS_END
#endif
