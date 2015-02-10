// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO;

internal static partial class Interop
{
    internal static partial class procfs
    {
        internal const string RootPath = "/proc/";
        internal const string ProcUptimeFilePath = RootPath + "uptime";
        internal const string StatFileName = "/stat";
        internal const string TaskDirectoryName = "/task/";

        internal struct ParsedStat
        {
            // Commented out fields are available in the stat data file but 
            // are currently not used.  If/when needed, they can be uncommented,
            // and the corresponding entry can be added back to StatParser, replacing
            // the MoveNext() with the appropriate ParseNext* call and assignment.

            internal int pid;
            internal string comm;
            internal char state;
            //internal int ppid;
            //internal int pgrp;
            internal int session;
            //internal int tty_nr;
            //internal int tpgid;
            //internal uint flags;
            //internal ulong minflt;
            //internal ulong cminflt;
            //internal ulong majflt;
            //internal ulong cmajflt;
            internal ulong utime;
            internal ulong stime;
            //internal long cutime;
            //internal long cstime;
            //internal long priority;
            internal long nice;
            //internal long num_threads;
            //internal long itrealvalue;
            internal ulong starttime;
            internal ulong vsize;
            internal long rss;
            internal ulong rsslim;
            //internal ulong startcode;
            //internal ulong endcode;
            internal ulong startstack;
            //internal ulong kstkesp;
            //internal ulong kstkeip;
            //internal ulong signal;
            //internal ulong blocked;
            //internal ulong sigignore;
            //internal ulong sigcatch;
            //internal ulong wchan;
            //internal ulong nswap;
            //internal ulong cnswap;
            //internal int exit_signal;
            //internal int processor;
            //internal uint rt_priority;
            //internal uint policy;
            //internal ulong delayacct_blkio_ticks;
            //internal ulong guest_time;
            //internal long cguest_time;
        }

        internal static string GetStatFilePathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + StatFileName;
        }

        internal static string GetTaskDirectoryPathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + TaskDirectoryName;
        }

        private static string GetStatFilePathForThread(int pid, int tid)
        {
            // Perf note: Calling GetTaskDirectoryPathForProcess will allocate a string,
            // which we then use in another Concat call to produce another string.  The straightforward alternative,
            // though, since we have five input strings, is to use the string.Concat overload that takes a params array. 
            // This results in allocating not only the params array but also a defensive copy inside of Concat,
            // which means allocating two five-element arrays.  This two-string approach will result not only in fewer 
            // allocations, but also typically in less memory allocated, and it's a bit more maintainable.
            return GetTaskDirectoryPathForProcess(pid) + tid.ToString(CultureInfo.InvariantCulture) + StatFileName;
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
            StatParser parser = new StatParser(statFileContents);
            ParsedStat results = default(ParsedStat);

            results.pid = parser.ParseNextInt32();
            results.comm = parser.ParseNextCommString();
            results.state = parser.ParseNextChar();
            parser.MoveNext(); // ppid
            parser.MoveNext(); // pgrp
            results.session = parser.ParseNextInt32();
            parser.MoveNext(); // tty_nr
            parser.MoveNext(); // tpgid
            parser.MoveNext(); // flags
            parser.MoveNext(); // majflt
            parser.MoveNext(); // cmagflt
            parser.MoveNext(); // minflt
            parser.MoveNext(); // cminflt
            results.utime = parser.ParseNextUInt64();
            results.stime = parser.ParseNextUInt64();
            parser.MoveNext(); // cutime
            parser.MoveNext(); // cstime
            parser.MoveNext(); // priority
            results.nice = parser.ParseNextInt64();
            parser.MoveNext(); // num_threads
            parser.MoveNext(); // itrealvalue
            results.starttime = parser.ParseNextUInt64();
            results.vsize = parser.ParseNextUInt64();
            results.rss = parser.ParseNextInt64();
            results.rsslim = parser.ParseNextUInt64();
            parser.MoveNext(); // startcode
            parser.MoveNext(); // endcode
            results.startstack = parser.ParseNextUInt64();
            parser.MoveNext(); // kstkesp
            parser.MoveNext(); // kstkeip
            parser.MoveNext(); // signal
            parser.MoveNext(); // blocked
            parser.MoveNext(); // sigignore
            parser.MoveNext(); // sigcatch
            parser.MoveNext(); // wchan
            parser.MoveNext(); // nswap
            parser.MoveNext(); // cnswap
            parser.MoveNext(); // exit_signal
            parser.MoveNext(); // processor
            parser.MoveNext(); // rt_priority
            parser.MoveNext(); // policy
            parser.MoveNext(); // delayacct_blkio_ticks
            parser.MoveNext(); // guest_time
            parser.MoveNext(); // cguest_time

            return results;
        }

        private struct StatParser
        {
            private readonly string _str;
            private int _startIndex;
            private int _endIndex;

            internal StatParser(string str)
            {
                _str = str;
                _startIndex = -1;
                _endIndex = -1;
            }

            internal void MoveNext()
            {
                if (_endIndex >= _str.Length)
                {
                    ThrowForInvalidData();
                }

                int nextSpace = _str.IndexOf(' ', _endIndex + 1);
                _startIndex = _endIndex + 1;
                _endIndex = nextSpace >= 0 ? nextSpace : _str.Length;
            }

            private string MoveAndExtractNext()
            {
                MoveNext();
                return _str.Substring(_startIndex, _endIndex - _startIndex);
            }

            internal int ParseNextInt32()
            {
                int result;
                if (!int.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    ThrowForInvalidData();
                }
                return result;
            }

            internal long ParseNextInt64()
            {
                long result;
                if (!long.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    ThrowForInvalidData();
                }
                return result;
            }

            internal uint ParseNextUInt32()
            {
                uint result;
                if (!uint.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    ThrowForInvalidData();
                }
                return result;
            }

            internal ulong ParseNextUInt64()
            {
                ulong result;
                if (!ulong.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    ThrowForInvalidData();
                }
                return result;
            }

            internal char ParseNextChar()
            {
                char result;
                if (!char.TryParse(MoveAndExtractNext(), out result))
                {
                    ThrowForInvalidData();
                }
                return result;
            }

            internal string ParseNextCommString()
            {
                MoveNext();
                if (_str[_startIndex] != '(' || _str[_endIndex - 1] != ')')
                {
                    ThrowForInvalidData();
                }
                return _str.Substring(_startIndex + 1, _endIndex - _startIndex - 2);
            }

            private static void ThrowForInvalidData()
            {
                throw new InvalidDataException();
            }
        }
    }
}
