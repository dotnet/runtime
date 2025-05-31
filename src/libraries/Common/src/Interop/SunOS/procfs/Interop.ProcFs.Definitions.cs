// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

// C# equivalents for <sys/procfs.h> structures. See: struct lwpsinfo, struct psinfo.
// We read directly onto these from procfs, so the layouts and sizes of these structures
// must _exactly_ match those in <sys/procfs.h>

// analyzer incorrectly flags fixed buffer length const
// (https://github.com/dotnet/roslyn/issues/37593)
#pragma warning disable CA1823

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal const string RootPath = "/proc/";
        private const string psinfoFileName = "/psinfo";
        private const string lwpDirName = "/lwp";
        private const string lwpsinfoFileName = "/lwpsinfo";

        // Constants from sys/procfs.h
        private const int PRARGSZ = 80;
        private const int PRCLSZ = 8;
        private const int PRFNSZ = 16;

        [StructLayout(LayoutKind.Sequential)]
        internal struct @timestruc_t
        {
            public long tv_sec;
            public long tv_nsec;
        }

        // lwp ps(1) information file.  /proc/<pid>/lwp/<lwpid>/lwpsinfo
        // Equivalent to sys/procfs.h struct lwpsinfo
        // "unsafe" because it has fixed sized arrays.
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct @lwpsinfo
        {
            private     int     pr_flag;        /* lwp flags (DEPRECATED; do not use) */
            public      uint    pr_lwpid;       /* lwp id */
            private     long    pr_addr;        /* internal address of lwp */
            private     long    pr_wchan;       /* wait addr for sleeping lwp */
            public      byte    pr_stype;       /* synchronization event type */
            public      byte    pr_state;       /* numeric lwp state */
            public      byte    pr_sname;       /* printable character for pr_state */
            public      byte    pr_nice;        /* nice for cpu usage */
            private     short   pr_syscall;     /* system call number (if in syscall) */
            private     byte    pr_oldpri;      /* pre-SVR4, low value is high priority */
            private     byte    pr_cpu;         /* pre-SVR4, cpu usage for scheduling */
            public      int     pr_pri;         /* priority, high value is high priority */
            private     ushort  pr_pctcpu;      /* fixed pt. % of recent cpu time */
            private     ushort  pr_pad;
            public      timestruc_t pr_start;   /* lwp start time, from the epoch */
            public      timestruc_t pr_time;    /* usr+sys cpu time for this lwp */
            private     fixed byte pr_clname[PRCLSZ];   /* scheduling class name */
            private     fixed byte pr_name[PRFNSZ];     /* name of system lwp */
            private     int     pr_onpro;               /* processor which last ran this lwp */
            private     int     pr_bindpro;     /* processor to which lwp is bound */
            private     int     pr_bindpset;    /* processor set to which lwp is bound */
            private     int     pr_lgrp;        /* lwp home lgroup */
            private     fixed int       pr_filler[4];   /* reserved for future use */
        }
        private const int PR_LWPSINFO_SIZE = 128; // for debug assertions

        // process ps(1) information file.  /proc/<pid>/psinfo
        // Equivalent to sys/procfs.h struct psinfo
        // "unsafe" because it has fixed sized arrays.
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct @psinfo
        {
            private     int     pr_flag;        /* process flags (DEPRECATED; do not use) */
            public      int     pr_nlwp;        /* number of active lwps in the process */
            public      int     pr_pid;         /* unique process id */
            public      int     pr_ppid;        /* process id of parent */
            public      int     pr_pgid;        /* pid of process group leader */
            public      int     pr_sid;         /* session id */
            public      uint    pr_uid;         /* real user id */
            public      uint    pr_euid;        /* effective user id */
            public      uint    pr_gid;         /* real group id */
            public      uint    pr_egid;        /* effective group id */
            private     long    pr_addr;        /* address of process */
            public      ulong   pr_size;        /* size of process image in Kbytes */
            public      ulong   pr_rssize;      /* resident set size in Kbytes */
            private     ulong   pr_pad1;
            private     ulong   pr_ttydev;      /* controlling tty device (or PRNODEV) */
            private     ushort  pr_pctcpu;      /* % of recent cpu time used by all lwps */
            private     ushort  pr_pctmem;      /* % of system memory used by process */
            public      timestruc_t pr_start;   /* process start time, from the epoch */
            public      timestruc_t pr_time;    /* usr+sys cpu time for this process */
            public      timestruc_t pr_ctime;   /* usr+sys cpu time for reaped children */
            public      fixed byte pr_fname[PRFNSZ];    /* name of execed file */
            public      fixed byte pr_psargs[PRARGSZ];  /* initial characters of arg list */
            public      int     pr_wstat;       /* if zombie, the wait() status */
            public      int     pr_argc;        /* initial argument count */
            private     long pr_argv;   /* address of initial argument vector */
            private     long pr_envp;   /* address of initial environment vector */
            private     byte    pr_dmodel;      /* data model of the process */
            private     fixed byte pr_pad2[3];
            public      int     pr_taskid;      /* task id */
            public      int     pr_projid;      /* project id */
            public      int     pr_nzomb;       /* number of zombie lwps in the process */
            public      int     pr_poolid;      /* pool id */
            public      int     pr_zoneid;      /* zone id */
            public      int     pr_contract;    /* process contract */
            private     fixed int pr_filler[1]; /* reserved for future use */
            public      lwpsinfo pr_lwp;        /* information for representative lwp */
            // C# magic: Accessor method to get a Span for pr_psargs[]
            // Does not affect the size or layout of this struct.
            internal     ReadOnlySpan<byte> PsArgsSpan =>
                MemoryMarshal.CreateReadOnlySpan(ref pr_psargs[0], PRARGSZ);
        }
        private const int PR_PSINFO_SIZE = 416; // for debug assertions

        // Ouput type for TryGetThreadInfoById()
        internal struct ThreadInfo
        {
            internal uint Tid;
            internal int Priority;
            internal int NiceVal;
            internal char Status;
            internal Interop.Sys.TimeSpec StartTime;
            internal Interop.Sys.TimeSpec CpuTotalTime; // user+sys
            // add more fields when needed.
        }

        // Ouput type for TryGetProcessInfoById()
        internal struct ProcessInfo
        {
            internal int Pid;
            internal int ParentPid;
            internal int SessionId;
            internal int Priority;
            internal int NiceVal;
            internal nuint VirtualSize;
            internal nuint ResidentSetSize;
            internal Interop.Sys.TimeSpec StartTime;
            internal Interop.Sys.TimeSpec CpuTotalTime; // user+sys
            internal string? Args;
            // add more fields when needed.
        }

        internal static string GetInfoFilePathForProcess(int pid) =>
            $"{RootPath}{(uint)pid}{psinfoFileName}";

        internal static string GetLwpDirForProcess(int pid) =>
            $"{RootPath}{(uint)pid}{lwpDirName}";

        internal static string GetInfoFilePathForThread(int pid, int tid) =>
            $"{RootPath}{(uint)pid}{lwpDirName}/{(uint)tid}{lwpsinfoFileName}";

    }
}
