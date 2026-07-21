// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CA1823 // unused fields are present to match the native struct layout

internal static partial class Interop
{
    internal static partial class Process
    {
        // Constants from sys/sysctl.h
        private const int CTL_KERN = 1;
        private const int KERN_PROC = 66;
        private const int KERN_PROC_ALL = 0;            // everything but kernel threads
        private const int KERN_PROC_PID = 1;            // by process id
        private const int KERN_PROC_SHOW_THREADS = unchecked((int)0x40000000); // also return threads
        private const int KERN_PROC_ARGS = 55;          // node: proc args and env
        private const int KERN_PROC_ARGV = 1;           // KERN_PROC_ARGS subtype: argv

        // Constants from sys/sysctl.h that determine the fixed-size members of kinfo_proc
        private const int KI_NGROUPS = 16;
        private const int KI_MAXCOMLEN = 24;    // _MAXCOMLEN, includes NUL
        private const int KI_WMESGLEN = 8;
        private const int KI_MAXLOGNAME = 32;
        private const int KI_EMULNAMELEN = 8;

        // From sys/sysctl.h: "struct kinfo_proc". OpenBSD guarantees a stable binary
        // layout for these members (new members are only appended to the end), and the
        // members use 8-byte alignment, matching the natural alignment used here.
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct @kinfo_proc
        {
            private ulong p_forw;               /* PTR: linked run/sleep queue. */
            private ulong p_back;
            private ulong p_paddr;              /* PTR: address of proc */
            private ulong p_addr;               /* PTR: Kernel virtual addr of u-area */
            private ulong p_fd;                 /* PTR: Ptr to open files structure. */
            private ulong p_stats;              /* unused, always zero. */
            private ulong p_limit;              /* PTR: Process limits. */
            private ulong p_vmspace;            /* PTR: Address space. */
            private ulong p_sigacts;            /* PTR: Signal actions, state */
            private ulong p_sess;               /* PTR: session pointer */
            private ulong p_tsess;              /* PTR: tty session pointer */
            private ulong p_ru;                 /* PTR: Exit information. XXX */

            private int p_eflag;                /* LONG: extra kinfo_proc flags */
            private int p_exitsig;              /* unused, always zero. */
            private int p_flag;                 /* INT: P_* flags. */

            public int p_pid;                   /* PID_T: Process identifier. */
            public int p_ppid;                  /* PID_T: Parent process id */
            public int p_sid;                   /* PID_T: session id */
            private int p__pgid;                /* PID_T: process group id */
            private int p_tpgid;                /* PID_T: tty process group id */

            public uint p_uid;                  /* UID_T: effective user id */
            private uint p_ruid;                /* UID_T: real user id */
            public uint p_gid;                  /* GID_T: effective group id */
            private uint p_rgid;                /* GID_T: real group id */

            private GroupsBuffer p_groups;      /* GID_T: groups */
            private short p_ngroups;            /* SHORT: number of groups */

            private short p_jobc;               /* SHORT: job control counter */
            private uint p_tdev;                /* DEV_T: controlling tty dev */

            private uint p_estcpu;              /* U_INT: Time averaged value of p_cpticks. */
            private uint p_rtime_sec;           /* STRUCT TIMEVAL: Real time. */
            private uint p_rtime_usec;          /* STRUCT TIMEVAL: Real time. */
            private int p_cpticks;              /* INT: Ticks of cpu time. */
            public uint p_pctcpu;               /* FIXPT_T: %cpu for this process */
            private uint p_swtime;              /* unused, always zero */
            private uint p_slptime;             /* U_INT: Time since last blocked. */
            private int p_schedflags;           /* INT: PSCHED_* flags */

            private ulong p_uticks;             /* U_QUAD_T: Statclock hits in user mode. */
            private ulong p_sticks;             /* U_QUAD_T: Statclock hits in system mode. */
            private ulong p_iticks;             /* U_QUAD_T: Statclock hits processing intr. */

            private ulong p_tracep;             /* PTR: Trace to vnode or file */
            private int p_traceflag;            /* INT: Kernel trace points. */

            private int p_holdcnt;              /* INT: If non-zero, don't swap. */

            private int p_siglist;              /* INT: Signals arrived but not delivered. */
            private uint p_sigmask;             /* SIGSET_T: Current signal mask. */
            private uint p_sigignore;           /* SIGSET_T: Signals being ignored. */
            private uint p_sigcatch;            /* SIGSET_T: Signals being caught by user. */

            private sbyte p_stat;               /* CHAR: S* process status (from LWP). */
            private byte p_priority;            /* U_CHAR: Process priority. */
            private byte p_usrpri;              /* U_CHAR: User-priority. */
            public byte p_nice;                 /* U_CHAR: Process "nice" value. */

            private ushort p_xstat;             /* U_SHORT: Exit status for wait; also stop signal. */
            private ushort p_spare;             /* U_SHORT: unused */

            public fixed byte p_comm[KI_MAXCOMLEN];     /* command name */

            private WmesgBuffer p_wmesg;        /* wchan message */
            private ulong p_wchan;              /* PTR: sleep address. */

            private LoginBuffer p_login;        /* setlogin() name */

            public int p_vm_rssize;             /* SEGSZ_T: current resident set size in pages */
            public int p_vm_tsize;              /* SEGSZ_T: text size (pages) */
            public int p_vm_dsize;              /* SEGSZ_T: data size (pages) */
            public int p_vm_ssize;              /* SEGSZ_T: stack size (pages) */

            private long p_uvalid;              /* CHAR: following p_u* members are valid */
            public ulong p_ustart_sec;          /* STRUCT TIMEVAL: starting time. */
            public uint p_ustart_usec;          /* STRUCT TIMEVAL: starting time. */

            public uint p_uutime_sec;           /* STRUCT TIMEVAL: user time. */
            public uint p_uutime_usec;          /* STRUCT TIMEVAL: user time. */
            public uint p_ustime_sec;           /* STRUCT TIMEVAL: system time. */
            public uint p_ustime_usec;          /* STRUCT TIMEVAL: system time. */

            public ulong p_uru_maxrss;          /* LONG: max resident set size (kilobytes). */
            private ulong p_uru_ixrss;          /* LONG: integral shared memory size. */
            private ulong p_uru_idrss;          /* LONG: integral unshared data ". */
            private ulong p_uru_isrss;          /* LONG: integral unshared stack ". */
            private ulong p_uru_minflt;         /* LONG: page reclaims. */
            private ulong p_uru_majflt;         /* LONG: page faults. */
            private ulong p_uru_nswap;          /* LONG: swaps. */
            private ulong p_uru_inblock;        /* LONG: block input operations. */
            private ulong p_uru_oublock;        /* LONG: block output operations. */
            private ulong p_uru_msgsnd;         /* LONG: messages sent. */
            private ulong p_uru_msgrcv;         /* LONG: messages received. */
            private ulong p_uru_nsignals;       /* LONG: signals received. */
            private ulong p_uru_nvcsw;          /* LONG: voluntary context switches. */
            private ulong p_uru_nivcsw;         /* LONG: involuntary ". */

            private uint p_uctime_sec;          /* STRUCT TIMEVAL: child u+s time. */
            private uint p_uctime_usec;         /* STRUCT TIMEVAL: child u+s time. */
            private uint p_psflags;             /* UINT: PS_* flags on the process. */
            private uint p_acflag;              /* UINT: Accounting flags. */
            private uint p_svuid;               /* UID_T: saved user id */
            private uint p_svgid;               /* GID_T: saved group id */
            private EmulNameBuffer p_emul;      /* syscall emulation name */
            private ulong p_rlim_rss_cur;       /* RLIM_T: soft limit for rss */
            private ulong p_cpuid;              /* LONG: CPU id */
            public ulong p_vm_map_size;         /* VSIZE_T: virtual size */
            public int p_tid;                   /* PID_T: Thread identifier. */
            private uint p_rtableid;            /* U_INT: Routing table identifier. */

            private ulong p_pledge;             /* U_INT64_T: Pledge flags. */
            private NameBuffer p_name;          /* thread name */

            [InlineArray(KI_NGROUPS)]
            private struct GroupsBuffer
            {
                private uint _element0;
            }

            [InlineArray(KI_WMESGLEN)]
            private struct WmesgBuffer
            {
                private byte _element0;
            }

            [InlineArray(KI_MAXLOGNAME)]
            private struct LoginBuffer
            {
                private byte _element0;
            }

            [InlineArray(KI_EMULNAMELEN)]
            private struct EmulNameBuffer
            {
                private byte _element0;
            }

            [InlineArray(KI_MAXCOMLEN)]
            private struct NameBuffer
            {
                private byte _element0;
            }
        }

        /// <summary>
        /// Gets information about processes.
        /// </summary>
        /// <param name="pid">The PID of the process to query, or 0 to enumerate all processes.</param>
        /// <param name="threads">When querying a single process, also return its threads.</param>
        /// <param name="count">The number of kinfo_proc entries returned.</param>
        public static unsafe kinfo_proc* GetProcInfo(int pid, bool threads, out int count)
        {
            // OpenBSD's KERN_PROC sysctl mib carries the element size and count inline:
            // { CTL_KERN, KERN_PROC, op, arg, sizeof(kinfo_proc), elem_count }.
            int op = pid == 0
                ? KERN_PROC_ALL
                : KERN_PROC_PID | (threads ? KERN_PROC_SHOW_THREADS : 0);
            int arg = pid == 0 ? 0 : pid;

            // The kernel bounds the result by the supplied buffer size, so request the
            // maximum element count and let Sysctl probe, allocate, and grow the buffer.
            ReadOnlySpan<int> sysctlName = [CTL_KERN, KERN_PROC, op, arg, sizeof(kinfo_proc), int.MaxValue];

            byte* pBuffer = null;
            uint bytesLength = 0;
            Interop.Sys.Sysctl(sysctlName, ref pBuffer, ref bytesLength);

            count = (int)(bytesLength / (uint)sizeof(kinfo_proc));

            // Buffer ownership transferred to the caller
            return (kinfo_proc*)pBuffer;
        }
    }
}
