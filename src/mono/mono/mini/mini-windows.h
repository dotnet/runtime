/**
 * \file
 */

#ifndef __MONO_MINI_WINDOWS_H__
#define __MONO_MINI_WINDOWS_H__

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include "windows.h"
#include "mini.h"
#include "mono/utils/mono-context.h"

gboolean
mono_setup_thread_context(DWORD thread_id, MonoContext *mono_context);
#endif /* HOST_WIN32 */
#endif /* __MONO_MINI_WINDOWS_H__ */
