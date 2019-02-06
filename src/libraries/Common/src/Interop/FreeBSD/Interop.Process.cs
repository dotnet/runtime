// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;


internal static partial class Interop
{
    internal static partial class Process
    {
        private const ulong SecondsToNanoSeconds = 1000000000;

        // Constants from sys/syslimits.h
        private const int PATH_MAX  = 1024;

        // Constants from sys/user.h
        private const int TDNAMLEN = 16;
        private const int WMESGLEN = 8;
        private const int LOGNAMELEN = 17;
        private const int LOCKNAMELEN = 8;
        private const int COMMLEN = 19;
        private const int KI_EMULNAMELEN = 16;
        private const int LOGINCLASSLEN = 17;
        private const int KI_NGROUPS = 16;

        private const int KI_NSPARE_INT = 4;
        private const int KI_NSPARE_LONG = 12;
        private const int KI_NSPARE_PTR = 6;


        // Constants from sys/_sigset.h
        private const int _SIG_WORDS = 4;

        // Constants from sys/sysctl.h
        private const int CTL_KERN = 1;
        private const int KERN_PROC = 14;
        private const int KERN_PROC_PATHNAME = 12;
        private const int KERN_PROC_PROC = 8;
        private const int KERN_PROC_ALL = 0; 
        private const int KERN_PROC_PID  = 1;
        private const int KERN_PROC_INC_THREAD = 16;

        // Constants from proc_info.h
        private const int MAXTHREADNAMESIZE = 64;
        private const int PROC_PIDTASKALLINFO = 2;
        private const int PROC_PIDTHREADINFO = 5;
        private const int PROC_PIDLISTTHREADS = 6;
        private const int PROC_PIDPATHINFO_MAXSIZE = 4 * PATH_MAX;

        internal struct proc_stats
        {
            internal long startTime;        /* time_t */
            internal int nice;
            internal ulong userTime;        /* in ticks */
            internal ulong systemTime;      /* in ticks */
        }

        // From sys/_sigset.h
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct sigset_t
        {
            private fixed int bits[4];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct uid_t
        {
            public uint id;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct gid_t
        {
            public uint id;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct timeval
        {
            public IntPtr tv_sec;
            public IntPtr tv_usec;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct vnode
        {
            public long tv_sec;
            public long tv_usec;
        }

        // sys/resource.h
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct rusage
        {
            public timeval ru_utime;        /* user time used */
            public timeval ru_stime;        /* system time used */
            public long ru_maxrss;          /* max resident set size */
            private long ru_ixrss;          /* integral shared memory size */
            private long ru_idrss;          /* integral unshared data " */
            private long ru_isrss;          /* integral unshared stack " */
            private long ru_minflt;         /* page reclaims */
            private long ru_majflt;         /* page faults */
            private long ru_nswap;          /* swaps */
            private long ru_inblock;        /* block input operations */
            private long ru_oublock;        /* block output operations */
            private long ru_msgsnd;         /* messages sent */
            private long ru_msgrcv;         /* messages received */
            private long ru_nsignals;       /* signals received */
            private long ru_nvcsw;          /* voluntary context switches */
            private long ru_nivcsw;         /* involuntary " */
        }

        // From  sys/user.h
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct kinfo_proc
        {
            public int ki_structsize;                   /* size of this structure */
            private int ki_layout;                      /* reserved: layout identifier */
            private void* ki_args;                      /* address of command arguments */
            private void* ki_paddr;                     /* address of proc */
            private void* ki_addr;                      /* kernel virtual addr of u-area */
            private vnode* ki_tracep;                   /* pointer to trace file */
            private vnode* ki_textvp;                   /* pointer to executable file */
            private void* ki_fd;                        /* pointer to open file info */
            private void* ki_vmspace;                   /* pointer to kernel vmspace struct */
            private void* ki_wchan;                     /* sleep address */
            public int ki_pid;                          /* Process identifier */
            public int ki_ppid;                         /* parent process id */
            private int ki_pgid;                        /* process group id */
            private int ki_tpgid;                       /* tty process group id */
            public int ki_sid;                          /* Process session ID */
            public int ki_tsid;                         /* Terminal session ID */
            private short ki_jobc;                      /* job control counter */
            private short ki_spare_short1;              /* unused (just here for alignment) */
            private int ki_tdev;                        /* controlling tty dev */
            private sigset_t ki_siglist;                /* Signals arrived but not delivered */
            private sigset_t ki_sigmask;                /* Current signal mask */
            private sigset_t ki_sigignore;              /* Signals being ignored */
            private sigset_t ki_sigcatch;               /* Signals being caught by user */
            public uid_t ki_uid;                        /* effective user id */
            private uid_t ki_ruid;                      /* Real user id */
            private uid_t ki_svuid;                     /* Saved effective user id */
            private gid_t ki_rgid;                      /* Real group id */
            private gid_t ki_svgid;                     /* Saved effective group id */
            private short ki_ngroups;                   /* number of groups */
            private short ki_spare_short2;              /* unused (just here for alignment) */
            private fixed uint ki_groups[KI_NGROUPS];   /* groups */
            public ulong ki_size;                       /* virtual size */
            public long ki_rssize;                      /* current resident set size in pages */
            private long ki_swrss;                      /* resident set size before last swap */
            private long ki_tsize;                      /* text size (pages) XXX */
            private long ki_dsize;                      /* data size (pages) XXX */
            private long ki_ssize;                      /* stack size (pages) */
            private ushort ki_xstat;                    /* Exit status for wait & stop signal */
            private ushort ki_acflag;                   /* Accounting flags */
            private uint ki_pctcpu;                     /* %cpu for process during ki_swtime */
            private uint ki_estcpu;                     /* Time averaged value of ki_cpticks */
            private uint ki_slptime;                    /* Time since last blocked */
            private uint ki_swtime;                     /* Time swapped in or out */
            private uint ki_cow;                        /* number of copy-on-write faults */
            private ulong ki_runtime;                   /* Real time in microsec */
            public timeval ki_start;                    /* starting time */
            private timeval ki_childtime;               /* time used by process children */
            private long ki_flag;                       /* P_* flags */
            private long ki_kiflag;                     /* KI_* flags (below) */
            private int ki_traceflag;                   /* Kernel trace points */
            private byte ki_stat;                       /* S* process status */
            public byte ki_nice;                        /* Process "nice" value */
            private byte ki_lock;                       /* Process lock (prevent swap) count */
            private byte ki_rqindex;                    /* Run queue index */
            private byte ki_oncpu_old;                  /* Which cpu we are on (legacy) */
            private byte ki_lastcpu_old;                /* Last cpu we were on (legacy) */
            public fixed byte ki_tdname[TDNAMLEN+1];    /* thread name */
            private fixed byte ki_wmesg[WMESGLEN+1];    /* wchan message */
            private fixed byte ki_login[LOGNAMELEN+1];  /* setlogin name */
            private fixed byte ki_lockname[LOCKNAMELEN+1]; /* lock name */
            public fixed byte ki_comm[COMMLEN+1];       /* command name */
            private fixed byte ki_emul[KI_EMULNAMELEN+1]; /* emulation name */
            private fixed byte ki_loginclass[LOGINCLASSLEN+1]; /* login class */
            private fixed byte ki_sparestrings[50];     /* spare string space */
            private fixed int ki_spareints[KI_NSPARE_INT]; /* spare room for growth */
            private int ki_oncpu;                       /* Which cpu we are on */
            private int ki_lastcpu;                     /* Last cpu we were on */
            private int ki_tracer;                      /* Pid of tracing process */
            private int ki_flag2;                       /* P2_* flags */
            private int ki_fibnum;                      /* Default FIB number */
            private uint ki_cr_flags;                   /* Credential flags */
            private int ki_jid;                         /* Process jail ID */
            public int ki_numthreads;                   /* XXXKSE number of threads in total */
            public int ki_tid;                          /* XXXKSE thread id */
            private fixed byte ki_pri[4];               /* process priority */
            public rusage ki_rusage;                    /* process rusage statistics */
            /* XXX - most fields in ki_rusage_ch are not (yet) filled in */
            private rusage ki_rusage_ch;                /* rusage of children processes */
            private void* ki_pcb;                       /* kernel virtual addr of pcb */
            private void* ki_kstack;                    /* kernel virtual addr of stack */
            private void* ki_udata;                     /* User convenience pointer */
            public void* ki_tdaddr;                     /* address of thread */

            private fixed long ki_spareptrs[KI_NSPARE_PTR];     /* spare room for growth */
            private fixed long ki_sparelongs[KI_NSPARE_LONG];   /* spare room for growth */
            private long ki_sflag;                              /* PS_* flags */
            private long ki_tdflags;                            /* XXXKSE kthread flag */
        }

        /// <summary>
        /// Queries the OS for the list of all running processes and returns the PID for each
        /// </summary>
        /// <returns>Returns a list of PIDs corresponding to all running processes</returns>
        internal static unsafe int[] ListAllPids()
        {
            int numProcesses = 0;
            int[] pids;
            kinfo_proc * entries = null;
            int idx;

            try
            {
                entries = GetProcInfo(0, false, out numProcesses);
                if (entries == null || numProcesses <= 0)
                {
                    throw new Win32Exception(SR.CantGetAllPids);
                }

                var list = new ReadOnlySpan<kinfo_proc>(entries, numProcesses);
                pids = new int[numProcesses];
                idx = 0;
                // walk through process list and skip kernel threads
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i].ki_ppid == 0)
                    {
                        // skip kernel threads
                        numProcesses-=1;
                    }
                    else
                    {
                        pids[idx] = list[i].ki_pid;
                        idx += 1;
                    }
                }
                // Remove extra elements
                Array.Resize<int>(ref pids, numProcesses);
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)entries);
            }
            return pids;
        }


        /// <summary>
        /// Gets executable name for process given it's PID
        /// </summary>
        /// <param name="pid">The PID of the process</param>
        public static unsafe string GetProcPath(int pid)
        {
            Span<int> sysctlName = stackalloc int[4];
            byte* pBuffer = null;
            int bytesLength = 0;

            sysctlName[0] = CTL_KERN;
            sysctlName[1] = KERN_PROC;
            sysctlName[2] = KERN_PROC_PATHNAME;
            sysctlName[3] = pid;

            int ret = Interop.Sys.Sysctl(sysctlName, ref pBuffer, ref bytesLength);
            if (ret  != 0 ) {
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(pBuffer,(int)bytesLength-1);
        }

        /// <summary>
        /// Gets information about process or thread(s)
        /// </summary>
        /// <param name="pid">The PID of the process. If PID is 0, this will return all processes</param>
        public static unsafe kinfo_proc* GetProcInfo(int pid, bool threads, out int count)
        {
            Span<int> sysctlName = stackalloc int[4];
            int bytesLength = 0;
            byte* pBuffer = null;
            kinfo_proc* kinfo = null;
            int ret;

            count = -1;

            if (pid == 0)
            {
                // get all processes
                sysctlName[3] = 0;
                sysctlName[2] = KERN_PROC_PROC;
            }
            else
            {
                // get specific process, possibly with threads
                sysctlName[3] = pid;
                sysctlName[2] = KERN_PROC_PID | (threads ? KERN_PROC_INC_THREAD : 0);
            }
            sysctlName[1] = KERN_PROC;
            sysctlName[0] = CTL_KERN;

            try
            {
                ret = Interop.Sys.Sysctl(sysctlName, ref pBuffer, ref bytesLength);
                if (ret != 0 ) {
                    throw new ArgumentOutOfRangeException(nameof(pid));
                }

                kinfo = (kinfo_proc*)pBuffer;
                if (kinfo->ki_structsize != sizeof(kinfo_proc))
                {
                    // failed consistency check 
                    throw new ArgumentOutOfRangeException(nameof(pid));
                }

                count = (int)bytesLength / sizeof(kinfo_proc);
            }
            catch
            {
                Marshal.FreeHGlobal((IntPtr)pBuffer);
                throw;
            }

            return kinfo;
        }

        /// <summary>
        /// Gets the process information for a given process
        /// </summary>
        /// <param name="pid">The PID (process ID) of the process</param>
        /// <returns>
        /// Returns a valid ProcessInfo struct for valid processes that the caller
        /// has permission to access; otherwise, returns null
        /// </returns>
        static public unsafe ProcessInfo GetProcessInfoById(int pid)
        {
            kinfo_proc* kinfo = null;
            int count;
            ProcessInfo info;

            // Negative PIDs are invalid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            try
            {
                kinfo = GetProcInfo(pid, true, out count);
                if (kinfo == null || count < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(pid));
                }

                var process = new ReadOnlySpan<kinfo_proc>(kinfo, count);

                // Get the process information for the specified pid
                info = new ProcessInfo();

                info.ProcessName = Marshal.PtrToStringAnsi((IntPtr)kinfo->ki_comm);
                info.BasePriority = kinfo->ki_nice;
                info.VirtualBytes = (long)kinfo->ki_size;
                info.WorkingSet = kinfo->ki_rssize;
                info.SessionId = kinfo ->ki_sid;

                for(int i = 0; i < process.Length; i++)
                {
                    var ti = new ThreadInfo()
                    {
                        _processId = pid,
                        _threadId = (ulong)process[i].ki_tid,
                        _basePriority = process[i].ki_nice,
                        _startAddress = (IntPtr)process[i].ki_tdaddr
                    };
                    info._threadInfoList.Add(ti);
                }
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)kinfo);
            }

            return info;
        }

        /// <summary>
        /// Gets the process information for a given process
        /// </summary>
        /// <param name="pid">The PID (process ID) of the process</param>
        /// <param name="tid">The TID (thread ID) of the process</param>
        /// <returns>
        /// Returns basic info about thread. If tis is 0, it will return
        /// info for process e.g. main thread.
        /// </returns>
        public unsafe static proc_stats GetThreadInfo(int pid, int tid)
        {
            proc_stats ret = new proc_stats();
            kinfo_proc* info = null;
            int count;

            try
            {
                info =  GetProcInfo(pid, (tid != 0), out count);
                if (info != null && count >= 1)
                {
                    if (tid == 0)
                    {
                        ret.startTime = (int)info->ki_start.tv_sec;
                        ret.nice = info->ki_nice;
                        ret.userTime = (ulong)info->ki_rusage.ru_utime.tv_sec * SecondsToNanoSeconds + (ulong)info->ki_rusage.ru_utime.tv_usec;
                        ret.systemTime = (ulong)info->ki_rusage.ru_stime.tv_sec * SecondsToNanoSeconds + (ulong)info->ki_rusage.ru_stime.tv_usec;
                    }
                    else
                    {
                        var list = new ReadOnlySpan<kinfo_proc>(info, count);
                        for(int i = 0; i < list.Length; i++)
                        {
                            if (list[i].ki_tid == tid)
                            {
                                ret.startTime = (int)list[i].ki_start.tv_sec;
                                ret.nice = list[i].ki_nice;
                                ret.userTime = (ulong)list[i].ki_rusage.ru_utime.tv_sec * SecondsToNanoSeconds + (ulong)list[i].ki_rusage.ru_utime.tv_usec;
                                ret.systemTime = (ulong)list[i].ki_rusage.ru_stime.tv_sec * SecondsToNanoSeconds + (ulong)list[i].ki_rusage.ru_stime.tv_usec;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)info);
            }

            return ret;
        }
    }
}
