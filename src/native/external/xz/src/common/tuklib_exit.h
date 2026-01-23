// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       tuklib_exit.h
/// \brief      Close stdout and stderr, and exit
/// \note       Requires tuklib_progname and tuklib_gettext modules
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef TUKLIB_EXIT_H
#define TUKLIB_EXIT_H

#include "tuklib_common.h"
TUKLIB_DECLS_BEGIN

#define tuklib_exit TUKLIB_SYMBOL(tuklib_exit)
tuklib_attr_noreturn
extern void tuklib_exit(int status, int err_status, int show_error);

TUKLIB_DECLS_END
#endif
