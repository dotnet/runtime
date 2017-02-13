/**
 * \file
 * mach support for x86
 *
 * Authors:
 *   Geoff Norton (gnorton@novell.com)
 *
 * (C) 2010 Ximian, Inc.
 */

#include <config.h>
#if defined(__MACH__)
#include <glib.h>
#include <mach/mach.h>
#include <mach/task.h>
#include <mach/mach_port.h>
#include <mach/thread_act.h>
#include <mach/thread_status.h>

#include <mono/utils/mono-mmap.h>

#include "mach-support.h"

kern_return_t
mono_mach_get_threads (thread_act_array_t *threads, guint32 *count)
{
	kern_return_t ret;

	do {
		ret = task_threads (current_task (), threads, count);
	} while (ret == KERN_ABORTED);

	return ret;
}

kern_return_t
mono_mach_free_threads (thread_act_array_t threads, guint32 count)
{
	return vm_deallocate(current_task (), (vm_address_t) threads, sizeof (thread_t) * count);
}
#endif
