/**
 * \file
 * Access the native error code
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */

#ifndef __MONO_ERRNO_H__
#define __MONO_ERRNO_H__

#include <errno.h>

// Enough indirection to do something else here, or log
inline static void
mono_set_errno (int errno_val)
{
	errno = errno_val;
}

#endif
