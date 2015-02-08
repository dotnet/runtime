// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

internal static partial class Interop
{
    internal static partial class procfs
    {
        internal const string RootPath = "/proc/";
        internal const string ProcUptimeFilePath = RootPath + "uptime";
        private const int ExpectedStatFileParts = 44;

#if DEBUG
        static procfs()
        {
            var fields = typeof(ParsedStat).GetTypeInfo().DeclaredFields;
            int fieldCount = 0;
            foreach (var item in fields)
            {
                fieldCount++;
            }
            Debug.Assert(ExpectedStatFileParts == fieldCount);
        }
#endif

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

        private static string GetStatFilePathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + "/stat";
        }

        internal static string GetTaskDirectoryPathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + "/task/";
        }

        private static string GetStatFilePathForThread(int pid, int tid)
        {
            // Perf note: Calling GetTaskDirectoryPathForProcess will allocate a string,
            // which we then use in another Concat call to produce another string.  The straightforward alternative,
            // though, since we have five input strings, is to use the string.Concat overload that takes a params array. 
            // This results in allocating not only the params array but also a defensive copy inside of Concat,
            // which means allocating two five-element arrays.  This two-string approach will result not only in fewer 
            // allocations, but also typically in less memory allocated, and it's a bit more maintainable.
            return GetTaskDirectoryPathForProcess(pid) + tid.ToString(CultureInfo.InvariantCulture) + "/stat";
        }

        internal static ParsedStat ReadStatFile(int pid)
        {
            string stat = File.ReadAllText(GetStatFilePathForProcess(pid));
            return ParseStatFile(stat);
        }

        internal static ParsedStat ReadStatFile(int pid, int tid)
        {
            string stat = File.ReadAllText(GetStatFilePathForThread(pid, tid));
            return ParseStatFile(stat);
        }

        private static ParsedStat ParseStatFile(string statFileContents)
        {
            ParsedStat results = default(ParsedStat);

            int index = 0;
            int i = 0;
            for (; i < ExpectedStatFileParts && index < statFileContents.Length; i++)
            {
                string part = GetNextPart(statFileContents, ref index);
                switch (i)
                {
                    case 0:
                        results.pid = ParseInt32(part);
                        break;
                    case 1:
                        string filename = part;
                        if (filename.StartsWith("(") && filename.EndsWith(")"))
                        {
                            filename = filename.Substring(1, filename.Length - 2);
                        }
                        else
                        {
                            Debug.Fail("Unexpected comm format");
                        }
                        results.comm = filename;
                        break;
                    case 2: 
                        bool parsed = char.TryParse(part, out results.state);
                        Debug.Assert(parsed);
                        break;
                    case 3:
                        results.ppid = ParseInt32(part);
                        break;
                    case 4:
                        results.pgrp = ParseInt32(part);
                        break;
                    case 5:
                        results.session = ParseInt32(part);
                        break;
                    case 6:
                        results.tty_nr = ParseInt32(part);
                        break;
                    case 7:
                        results.tpgid = ParseInt32(part);
                        break;
                    case 8:
                        results.flags = ParseUInt32(part);
                        break;
                    case 9:
                        results.majflt = ParseUInt64(part);
                        break;
                    case 10:
                        results.cmajflt = ParseUInt64(part);
                        break;
                    case 11:
                        results.minflt = ParseUInt64(part);
                        break;
                    case 12:
                        results.cminflt = ParseUInt64(part);
                        break;
                    case 13:
                        results.utime = ParseUInt64(part);
                        break;
                    case 14:
                        results.stime = ParseUInt64(part);
                        break;
                    case 15:
                        results.cutime = ParseInt64(part);
                        break;
                    case 16:
                        results.cstime = ParseInt64(part);
                        break;
                    case 17:
                        results.priority = ParseInt64(part);
                        break;
                    case 18:
                        results.nice = ParseInt64(part);
                        break;
                    case 19:
                        results.num_threads = ParseInt64(part);
                        break;
                    case 20:
                        results.itrealvalue = ParseInt64(part);
                        break;
                    case 21:
                        results.starttime = ParseUInt64(part);
                        break;
                    case 22:
                        results.vsize = ParseUInt64(part);
                        break;
                    case 23:
                        results.rss = ParseInt64(part);
                        break;
                    case 24:
                        results.rsslim = ParseUInt64(part);
                        break;
                    case 25:
                        results.startcode = ParseUInt64(part);
                        break;
                    case 26:
                        results.endcode = ParseUInt64(part);
                        break;
                    case 27:
                        results.startstack = ParseUInt64(part);
                        break;
                    case 28:
                        results.kstkesp = ParseUInt64(part);
                        break;
                    case 29:
                        results.kstkeip = ParseUInt64(part);
                        break;
                    case 30:
                        results.signal = ParseUInt64(part);
                        break;
                    case 31:
                        results.blocked = ParseUInt64(part);
                        break;
                    case 32:
                        results.sigignore = ParseUInt64(part);
                        break;
                    case 33:
                        results.sigcatch = ParseUInt64(part);
                        break;
                    case 34:
                        results.wchan = ParseUInt64(part);
                        break;
                    case 35:
                        results.nswap = ParseUInt64(part);
                        break;
                    case 36:
                        results.cnswap = ParseUInt64(part);
                        break;
                    case 37:
                        results.exit_signal = ParseInt32(part);
                        break;
                    case 38:
                        results.processor = ParseInt32(part);
                        break;
                    case 39:
                        results.rt_priority = ParseUInt32(part);
                        break;
                    case 40:
                        results.policy = ParseUInt32(part);
                        break;
                    case 41:
                        results.delayacct_blkio_ticks = ParseUInt64(part);
                        break;
                    case 42:
                        results.guest_time = ParseUInt64(part);
                        break;
                    case 43:
                        results.cguest_time = ParseInt64(part);
                        break;
                }
            }

            Debug.Assert(i == ExpectedStatFileParts);

            return results;
        }

        private static string GetNextPart(string str, ref int index)
        {
            string part;
            int nextSpace = str.IndexOf(' ', index);
            if (nextSpace < 0)
            {
                part = str.Substring(index);
                index = str.Length;
            }
            else
            {
                part = str.Substring(index, nextSpace - index);
                index = nextSpace + 1;
            }
            return part;
        }

        private static int ParseInt32(string str)
        {
            int result;
            bool parsed = int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            Debug.Assert(parsed);
            return result;
        }

        private static uint ParseUInt32(string str)
        {
            uint result;
            bool parsed = uint.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            Debug.Assert(parsed);
            return result;
        }

        private static long ParseInt64(string str)
        {
            long result;
            bool parsed = long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            Debug.Assert(parsed);
            return result;
        }

        private static ulong ParseUInt64(string str)
        {
            ulong result;
            bool parsed = ulong.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            Debug.Assert(parsed);
            return result;
        }
    }
}
