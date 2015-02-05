// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

internal static partial class Interop
{
    internal static partial class procfs
    {
        internal struct ParsedStat
        {
            internal int pid;
            internal string comm;
            internal char state;
            internal int ppid;
            internal int pgrp;
            internal int session;
            internal int tty_nr;
            internal int tpgid;
            internal uint flags;
            internal ulong minflt;
            internal ulong cminflt;
            internal ulong majflt;
            internal ulong cmajflt;
            internal ulong utime;
            internal ulong stime;
            internal long cutime;
            internal long cstime;
            internal long priority;
            internal long nice;
            internal long num_threads;
            internal long itrealvalue;
            internal ulong starttime;
            internal ulong vsize;
            internal long rss;
            internal ulong rsslim;
            internal ulong startcode;
            internal ulong endcode;
            internal ulong startstack;
            internal ulong kstkesp;
            internal ulong kstkeip;
            internal ulong signal;
            internal ulong blocked;
            internal ulong sigignore;
            internal ulong sigcatch;
            internal ulong wchan;
            internal ulong nswap;
            internal ulong cnswap;
            internal int exit_signal;
            internal int processor;
            internal uint rt_priority;
            internal uint policy;
            internal ulong delayacct_blkio_ticks;
            internal ulong guest_time;
            internal long cguest_time;
        }

        internal static ParsedStat ReadStat(int pid)
        {
            string stat = File.ReadAllText("/proc/" + pid.ToString() + "/stat").Trim();
            return ReadStat(stat);
        }

        internal static ParsedStat ReadStat(int pid, int tid)
        {
            string stat = File.ReadAllText("/proc/" + pid.ToString() + "/task/" + tid + "/stat").Trim();
            return ReadStat(stat);
        }

        private static ParsedStat ReadStat(string stat)
        {
            string[] parts = stat.Split(null);

            ParsedStat results = default(ParsedStat);
            for (int i = 0; i < 44 && i < parts.Length; i++)
            {
                string part = parts[i];
                switch (i)
                {
                    case 0:
                        int.TryParse(part, out results.pid);
                        break;
                    case 1:
                        string filename = part;
                        if (filename.StartsWith("(") && filename.EndsWith(")"))
                        {
                            filename = filename.Substring(1, filename.Length - 2);
                        }
                        results.comm = filename;
                        break;
                    case 2:
                        char.TryParse(part, out results.state);
                        break;
                    case 3:
                        int.TryParse(part, out results.ppid);
                        break;
                    case 4:
                        int.TryParse(part, out results.pgrp);
                        break;
                    case 5:
                        int.TryParse(part, out results.session);
                        break;
                    case 6:
                        int.TryParse(part, out results.tty_nr);
                        break;
                    case 7:
                        int.TryParse(part, out results.tpgid);
                        break;
                    case 8:
                        uint.TryParse(part, out results.flags);
                        break;
                    case 9:
                        ulong.TryParse(part, out results.majflt);
                        break;
                    case 10:
                        ulong.TryParse(part, out results.cmajflt);
                        break;
                    case 11:
                        ulong.TryParse(part, out results.minflt);
                        break;
                    case 12:
                        ulong.TryParse(part, out results.cminflt);
                        break;
                    case 13:
                        ulong.TryParse(part, out results.utime);
                        break;
                    case 14:
                        ulong.TryParse(part, out results.stime);
                        break;
                    case 15:
                        long.TryParse(part, out results.cutime);
                        break;
                    case 16:
                        long.TryParse(part, out results.cstime);
                        break;
                    case 17:
                        long.TryParse(part, out results.priority);
                        break;
                    case 18:
                        long.TryParse(part, out results.nice);
                        break;
                    case 19:
                        long.TryParse(part, out results.num_threads);
                        break;
                    case 20:
                        long.TryParse(part, out results.itrealvalue);
                        break;
                    case 21:
                        ulong.TryParse(part, out results.starttime);
                        break;
                    case 22:
                        ulong.TryParse(part, out results.vsize);
                        break;
                    case 23:
                        long.TryParse(part, out results.rss);
                        break;
                    case 24:
                        ulong.TryParse(part, out results.rsslim);
                        break;
                    case 25:
                        ulong.TryParse(part, out results.startcode);
                        break;
                    case 26:
                        ulong.TryParse(part, out results.endcode);
                        break;
                    case 27:
                        ulong.TryParse(part, out results.startstack);
                        break;
                    case 28:
                        ulong.TryParse(part, out results.kstkesp);
                        break;
                    case 29:
                        ulong.TryParse(part, out results.kstkeip);
                        break;
                    case 30:
                        ulong.TryParse(part, out results.signal);
                        break;
                    case 31:
                        ulong.TryParse(part, out results.blocked);
                        break;
                    case 32:
                        ulong.TryParse(part, out results.sigignore);
                        break;
                    case 33:
                        ulong.TryParse(part, out results.sigcatch);
                        break;
                    case 34:
                        ulong.TryParse(part, out results.wchan);
                        break;
                    case 35:
                        ulong.TryParse(part, out results.nswap);
                        break;
                    case 36:
                        ulong.TryParse(part, out results.cnswap);
                        break;
                    case 37:
                        int.TryParse(part, out results.exit_signal);
                        break;
                    case 38:
                        int.TryParse(part, out results.processor);
                        break;
                    case 39:
                        uint.TryParse(part, out results.rt_priority);
                        break;
                    case 40:
                        uint.TryParse(part, out results.policy);
                        break;
                    case 41:
                        ulong.TryParse(part, out results.delayacct_blkio_ticks);
                        break;
                    case 42:
                        ulong.TryParse(part, out results.guest_time);
                        break;
                    case 43:
                        long.TryParse(part, out results.cguest_time);
                        break;
                }
            }
            return results;
        }
    }
}
