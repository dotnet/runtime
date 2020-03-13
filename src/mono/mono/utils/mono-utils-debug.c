/* mono-utils-debug.c
 *
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <config.h>
#include <glib.h>
#include "mono-utils-debug.h"

#if defined (_WIN32)

#include <windows.h>

gboolean
mono_is_usermode_native_debugger_present (void)
{
	// This is just a few instructions and no syscall. It is very fast.
	// Kernel debugger is detected otherwise and is also useful for usermode debugging.
	// Mono managed debugger is detected otherwise.
	return IsDebuggerPresent () ? TRUE : FALSE;
}

#else

#include <unistd.h>
#include <errno.h>
#include <mono/utils/mono-errno.h>
#include <fcntl.h>
#if defined (__APPLE__)
#include <sys/sysctl.h>
#endif
#if defined (__NetBSD__)
#include <kvm.h>
#endif
#if defined (_AIX)
#include <procinfo.h>
#endif

static gboolean
mono_is_usermode_native_debugger_present_slow (void)
// PAL_IsDebuggerPresent
// based closely on with some local cleanup:
//   https://github.com/dotnet/coreclr/blob/master/src/pal/src/init/pal.cpp
//   https://raw.githubusercontent.com/dotnet/coreclr/f1c9dac3e2db2397e01cd2da3e8aaa4f81a80013/src/pal/src/init/pal.cpp
{
#if defined (__APPLE__)

	struct kinfo_proc info;
	size_t size = sizeof (info);
	memset (&info, 0, size);
	int mib [4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid () };
	return sysctl (mib, sizeof (mib) / sizeof (*mib), &info, &size, NULL, 0) == 0
		&& (info.kp_proc.p_flag & P_TRACED) != 0;

#elif defined (__linux__)

	int const status_fd = open ("/proc/self/status", O_RDONLY);
	if (status_fd == -1)
		return FALSE;

	char buf [4098];
	buf [0] = '\n'; // consider if the first line
	buf [1] = 0; // clear out garbage
	ssize_t const num_read = read (status_fd, &buf [1], sizeof(buf) - 2);
	close (status_fd);
	static const char TracerPid [ ] = "\nTracerPid:";
	if (num_read <= sizeof (TracerPid))
		return FALSE;

	buf [num_read + 1] = 0;
	char const * const tracer_pid = strstr (buf, TracerPid);
	return tracer_pid && atoi (tracer_pid + sizeof (TracerPid) - 1);

#elif defined (__NetBSD__)

	kvm_t * const kd = kvm_open (NULL, NULL, NULL, KVM_NO_FILES, "kvm_open");
	if (!kd)
		return FALSE;
	int count = 0;
	struct kinfo_proc const * const info = kvm_getprocs (kd, KERN_PROC_PID, getpid (), &count);
	gboolean const traced = info && count > 0 && (info->kp_proc.p_slflag & PSL_TRACED);
	kvm_close (kd);
	return traced;

#elif defined (_AIX)

	struct procentry64 proc;
	pid_t pid;
	pid = getpid ();
	getprocs64 (&proc, sizeof (proc), NULL, 0, &pid, 1);
	return (proc.pi_flags & STRC) != 0; // SMPTRACE or SWTED might work too

#else
	return FALSE; // FIXME Other operating systems.
#endif
}

// Cache because it is slow.
static gchar mono_is_usermode_native_debugger_present_cache; // 0:uninitialized 1:true 2:false

gboolean
mono_is_usermode_native_debugger_present (void)
{
	if (mono_is_usermode_native_debugger_present_cache == 0) {
		int er = errno;
		mono_is_usermode_native_debugger_present_cache = mono_is_usermode_native_debugger_present_slow () ? 1 : 2;
		mono_set_errno (er);
	}
	return mono_is_usermode_native_debugger_present_cache == 1;
}

#endif // fast uncached Window vs. slow cached the rest

#if 0 // test

int
#ifdef _MSC_VER
__cdecl
#endif
main ()
{
	printf ("mono_usermode_native_debugger_present:%d\n", mono_is_usermode_native_debugger_present ());
}

#endif
