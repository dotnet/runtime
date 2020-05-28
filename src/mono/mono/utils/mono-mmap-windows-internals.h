/**
 * \file
 */

#ifndef __MONO_UTILS_MMAP_WINDOWS_H__
#define __MONO_UTILS_MMAP_WINDOWS_H__

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include "mono/utils/mono-mmap.h"
#include "mono/utils/mono-mmap-internals.h"

int
mono_mmap_win_prot_from_flags (int flags);
#endif /* HOST_WIN32 */
#endif /* __MONO_UTILS_MMAP_WINDOWS_H__ */

