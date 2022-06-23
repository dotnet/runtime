/**
 * \file
 * Copyright 2008-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include <mono/utils/mono-proclib.h>

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <fcntl.h>

#ifdef HOST_WIN32
#include <windows.h>
#include <process.h>
#else
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SCHED_GETAFFINITY
#include <sched.h>
#endif
#ifdef HAVE_SYS_SYSCTL_H
#include <sys/sysctl.h>
#endif
#endif

int
mono_process_current_pid ()
{
#ifdef HOST_WIN32
	return (int) GetCurrentProcessId ();
#elif defined(HAVE_GETPID)
	return (int) getpid ();
#elif defined(HOST_WASI)
	return 0;
#else
#error getpid
#endif
}

#ifdef HAVE_SCHED_GETAFFINITY
#  ifndef HAVE_GNU_CPU_COUNT
static int
CPU_COUNT(cpu_set_t *set)
{
	int i, count = 0;

	for (int i = 0; i < CPU_SETSIZE; i++)
		if (CPU_ISSET(i, set))
			count++;
	return count;
}
#  endif
#endif

/**
 * mono_cpu_count:
 * \returns the number of processors on the system.
 */
int
mono_cpu_count (void)
{
#ifdef HOST_WIN32
	SYSTEM_INFO info;
	GetSystemInfo (&info);
	return info.dwNumberOfProcessors;
#else
#ifdef HOST_ANDROID
	/* Android tries really hard to save power by powering off CPUs on SMP phones which
	 * means the normal way to query cpu count returns a wrong value with userspace API.
	 * Instead we use /sys entries to query the actual hardware CPU count.
	 */
	int count = 0;
	char buffer[8] = {'\0'};
	int present = open ("/sys/devices/system/cpu/present", O_RDONLY);
	/* Format of the /sys entry is a cpulist of indexes which in the case
	 * of present is always of the form "0-(n-1)" when there is more than
	 * 1 core, n being the number of CPU cores in the system. Otherwise
	 * the value is simply 0
	 */
	if (present != -1 && read (present, (char*)buffer, sizeof (buffer)) > 3)
		count = strtol (((char*)buffer) + 2, NULL, 10);
	if (present != -1)
		close (present);
	if (count > 0)
		return count + 1;
#endif

#if defined(HOST_ARM) || defined (HOST_ARM64)

/*
 * Recap from Alexander Köplinger <alex.koeplinger@outlook.com>:
 *
 * When we merged the change from mono/mono PR #2722, we started seeing random failures on ARM in
 * the MonoTests.System.Threading.ThreadPoolTests.SetAndGetMaxThreads and
 * MonoTests.System.Threading.ManualResetEventSlimTests.Constructor_Defaults tests. Both
 * of those tests are dealing with Environment.ProcessorCount to verify some implementation
 * details.
 *
 * It turns out that on the Jetson TK1 board we use on public Jenkins and on ARM kernels
 * in general, the value returned by sched_getaffinity (or _SC_NPROCESSORS_ONLN) doesn't
 * contain CPUs/cores that are powered off for power saving reasons. This is contrary to
 * what happens on x86, where even cores in deep-sleep state are returned [1], [2]. This
 * means that we would get a processor count of 1 at one point in time and a higher value
 * when load increases later on as the system wakes CPUs.
 *
 * Various runtime pieces like the threadpool and also user code however relies on the
 * value returned by Environment.ProcessorCount e.g. for deciding how many parallel tasks
 * to start, thereby limiting the performance when that code thinks we only have one CPU.
 *
 * Talking to a few people, this was the reason why we changed to _SC_NPROCESSORS_CONF in
 * mono#1688 and why we added a special case for Android in mono@de3addc to get the "real"
 * number of processors in the system.
 *
 * Because of those issues Android/Dalvik also switched from _ONLN to _SC_NPROCESSORS_CONF
 * for the Java API Runtime.availableProcessors() too [3], citing:
 * > Traditionally this returned the number currently online, but many mobile devices are
 * able to take unused cores offline to save power, so releases newer than Android 4.2 (Jelly
 * Bean) return the maximum number of cores that could be made available if there were no
 * power or heat constraints.
 *
 * The problem with sticking to _SC_NPROCESSORS_CONF however is that it breaks down in
 * constrained environments like Docker or with an explicit CPU affinity set by the Linux
 * `taskset` command, They'd get a higher CPU count than can be used, start more threads etc.
 * which results in unnecessary context switches and overloaded systems. That's why we need
 * to respect sched_getaffinity.
 *
 * So while in an ideal world we would be able to rely on sched_getaffinity/_SC_NPROCESSORS_ONLN
 * to return the number of theoretically available CPUs regardless of power saving measures
 * everywhere, we can't do this on ARM.
 *
 * I think the pragmatic solution is the following:
 * * use sched_getaffinity (+ fallback to _SC_NPROCESSORS_ONLN in case of error) on x86. This
 * ensures we're inline with what OpenJDK [4] and CoreCLR [5] do
 * * use _SC_NPROCESSORS_CONF exclusively on ARM (I think we could eventually even get rid of
 * the HOST_ANDROID special case)
 *
 * Helpful links:
 *
 * [1] https://sourceware.org/ml/libc-alpha/2013-07/msg00383.html
 * [2] https://lists.01.org/pipermail/powertop/2012-September/000433.html
 * [3] https://android.googlesource.com/platform/libcore/+/750dc634e56c58d1d04f6a138734ac2b772900b5%5E1..750dc634e56c58d1d04f6a138734ac2b772900b5/
 * [4] https://bugs.openjdk.java.net/browse/JDK-6515172
 * [5] https://github.com/dotnet/coreclr/blob/7058273693db2555f127ce16e6b0c5b40fb04867/src/pal/src/misc/sysinfo.cpp#L148
 */

#if defined (_SC_NPROCESSORS_CONF) && defined (HAVE_SYSCONF)
	{
		int count = sysconf (_SC_NPROCESSORS_CONF);
		if (count > 0)
			return count;
	}
#endif

#else

#ifdef HAVE_SCHED_GETAFFINITY
	{
		cpu_set_t set;
		if (sched_getaffinity (mono_process_current_pid (), sizeof (set), &set) == 0)
			return CPU_COUNT (&set);
	}
#endif
#if defined (_SC_NPROCESSORS_ONLN) && defined (HAVE_SYSCONF)
	{
		int count = sysconf (_SC_NPROCESSORS_ONLN);
		if (count > 0)
			return count;
	}
#endif

#endif /* defined(HOST_ARM) || defined (HOST_ARM64) */

#ifdef HAVE_SYS_SYSCTL_H
	{
		int count;
		int mib [2];
		size_t len = sizeof (int);
		mib [0] = CTL_HW;
		mib [1] = HW_NCPU;
		if (sysctl (mib, 2, &count, &len, NULL, 0) == 0)
			return count;
	}
#endif
	/* FIXME: warn */
	return 1;
#endif
}
