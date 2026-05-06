// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/debugger.h>

#include <fcntl.h>
#include <string.h>
#include <stdlib.h>
#include <sys/types.h>

#ifndef _WIN32
#include <unistd.h>
#ifdef __FreeBSD__
#include <sys/user.h>
#endif
#endif

#if defined(_WIN32)
#include <windows.h>
#define MINIPAL_DEBUGGER_PRESENT_CHECK
#elif defined(__linux__)
#include <stdio.h>
#define MINIPAL_DEBUGGER_PRESENT_CHECK
#elif defined(__APPLE__) || defined(__FreeBSD__)
#include <sys/sysctl.h>
#include <sys/types.h>
#define MINIPAL_DEBUGGER_PRESENT_CHECK
#elif defined(__NetBSD__)
#include <kvm.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <sys/proc.h>
#define MINIPAL_DEBUGGER_PRESENT_CHECK
#elif defined(__sun)
#include <stdio.h>
#include <fcntl.h>
#include <procfs.h>
#include <errno.h>
#define MINIPAL_DEBUGGER_PRESENT_CHECK
#elif defined(_AIX)
#include <sys/proc.h>
#include <sys/types.h>
#include <sys/procfs.h>
#define MINIPAL_DEBUGGER_PRESENT_CHECK
#elif defined(__HAIKU__)
#include <OS.h>
#define MINIPAL_DEBUGGER_PRESENT_CHECK
#endif

bool minipal_can_check_for_native_debugger(void)
{
#if defined(MINIPAL_DEBUGGER_PRESENT_CHECK)
    return true;
#else
    return false;
#endif
}

bool minipal_is_native_debugger_present(void)
{
#if defined(_WIN32)
    return IsDebuggerPresent();

#elif defined(__linux__)
    bool debugger_present = false;
    char buf[2048];

    int status_fd = open("/proc/self/status", O_RDONLY);
    if (status_fd == -1)
    {
        return false;
    }
    ssize_t num_read = read(status_fd, buf, sizeof(buf) - 1);
    if (num_read > 0)
    {
        static const char TracerPid[] = "TracerPid:";
        char *tracer_pid;

        buf[num_read] = '\0';
        tracer_pid = strstr(buf, TracerPid);
        if (tracer_pid)
        {
            debugger_present = !!atoi(tracer_pid + sizeof(TracerPid) - 1);
        }
    }

    close(status_fd);
    return debugger_present;

#elif defined(__APPLE__) || defined(__FreeBSD__)
    struct kinfo_proc info;
    size_t size = sizeof(info);
    int mib[4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid() };
    if (sysctl(mib, sizeof(mib)/sizeof(*mib), &info, &size, NULL, 0) == 0)
    {
#if defined(__APPLE__)
        return ((info.kp_proc.p_flag & P_TRACED) != 0);
#else // FreeBSD
        return ((info.ki_flag & P_TRACED) != 0);
#endif
    }
    return false;

#elif defined(__NetBSD__)
    int traced;
    kvm_t *kd;
    int cnt;
    struct kinfo_proc *info;

    kd = kvm_open(NULL, NULL, NULL, KVM_NO_FILES, "kvm_open");
    if (kd == NULL)
        return false;

    info = kvm_getprocs(kd, KERN_PROC_PID, getpid(), &cnt);
    if (info == NULL || cnt < 1)
    {
        kvm_close(kd);
        return false;
    }

    traced = info->kp_proc.p_slflag & PSL_TRACED;
    kvm_close(kd);
    return traced != 0;

#elif defined(__sun)
    int fd;
    char statusFilename[64];
    snprintf(statusFilename, sizeof(statusFilename), "/proc/%d/status", getpid());
    fd = open(statusFilename, O_RDONLY);
    if (fd == -1)
    {
        return false;
    }

    pstatus_t status;
    ssize_t readResult;
    do
    {
        readResult = read(fd, &status, sizeof(status));
    } while (readResult == -1 && errno == EINTR);

    close(fd);
    return status.pr_flttrace.word[0] != 0;

#elif defined(_AIX)
    struct procentry64 proc;
    pid_t pid = getpid();
    getprocs64(&proc, sizeof(proc), NULL, 0, &pid, 1);
    return (proc.pi_flags & STRC) != 0; // SMPTRACE or SWTED might work too

#elif defined(__HAIKU__)
    team_info info;
    if (get_team_info(B_CURRENT_TEAM, &info) == B_OK)
    {
        return info.debugger_nub_thread > 0;
    }
    return false;

#else
    return false;
#endif
}
