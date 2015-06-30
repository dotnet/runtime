// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;

internal static partial class Interop
{
    internal static partial class procfs
    {
        internal const string RootPath = "/proc/";
        internal const string SelfExeFilePath = RootPath + "self/exe";
        internal const string ProcUptimeFilePath = RootPath + "uptime";
        private const string StatFileName = "/stat";
        private const string MapsFileName = "/maps";
        private const string TaskDirectoryName = "/task/";

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

        internal struct ParsedMapsModule
        {
            internal string FileName;
            internal KeyValuePair<long, long> AddressRange;
        }

        internal static string GetStatFilePathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + StatFileName;
        }

        internal static string GetMapsFilePathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + MapsFileName;
        }

        internal static string GetTaskDirectoryPathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + TaskDirectoryName;
        }

        internal static IEnumerable<ParsedMapsModule> ParseMapsModules(int pid)
        {
            try
            {
                return ParseMapsModulesCore(File.ReadLines(GetMapsFilePathForProcess(pid)));
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (UnauthorizedAccessException) { }

            return Array.Empty<ParsedMapsModule>();
        }

        private static IEnumerable<ParsedMapsModule> ParseMapsModulesCore(IEnumerable<string> lines)
        {
            Debug.Assert(lines != null);

            // Parse each line from the maps file into a ParsedMapsModule result
            foreach (string line in lines)
            {
                // Use a StringParser to avoid string.Split costs
                var parser = new StringParser(line, separator: ' ', skipEmpty: true);

                // Parse the address range 
                KeyValuePair<long, long> addressRange =  
                    parser.ParseRaw(delegate (string s, ref int start, ref int end)
                    {
                        long startingAddress = 0, endingAddress = 0;
                        int pos = s.IndexOf('-', start, end - start);
                        if (pos > 0)
                        {
                            string startingString = s.Substring(start, pos);
                            if (long.TryParse(startingString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startingAddress))
                            {
                                string endingString = s.Substring(pos + 1, end - (pos + 1));
                                long.TryParse(endingString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out endingAddress);
                            }
                        }
                        return new KeyValuePair<long, long>(startingAddress, endingAddress);
                    });

                // Parse the permissions (we only care about entries with 'r' and 'x' set)
                if (!parser.ParseRaw(delegate (string s, ref int start, ref int end)
                {
                    bool sawRead = false, sawExec = false;
                    for (int i = start; i < end; i++)
                    {
                        if (s[i] == 'r')
                            sawRead = true;
                        else if (s[i] == 'x')
                            sawExec = true;
                    }
                    return sawRead & sawExec;
                }))
                {
                    continue;
                }

                // Skip past the offset, dev, and inode fields
                parser.MoveNext();
                parser.MoveNext();
                parser.MoveNext();

                // Parse the pathname
                if (!parser.MoveNext())
                {
                    continue;
                }
                string pathname = parser.ExtractCurrent();

                // We only get here if a we have a non-empty pathname and
                // the permissions included both readability and executability.
                // Yield the result.
                yield return new ParsedMapsModule { FileName = pathname, AddressRange = addressRange };
            }
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
            return ParseStatFile(GetStatFilePathForProcess(pid));
        }

        internal static ParsedStat ReadStatFile(int pid, int tid)
        {
            return ParseStatFile(GetStatFilePathForThread(pid, tid));
        }

        private static ParsedStat ParseStatFile(string statFilePath)
        {
            string statFileContents = File.ReadAllText(statFilePath);

            var parser = new StringParser(statFileContents, ' ');
            var results = default(ParsedStat);

            results.pid = parser.ParseNextInt32();
            results.comm = parser.ParseRaw(delegate (string str, ref int startIndex, ref int endIndex)
            {
                if (str[startIndex] == '(')
                {
                    int i;
                    for (i = endIndex; i < str.Length && str[i - 1] != ')'; i++) ;
                    if (str[i - 1] == ')')
                    {
                        endIndex = i;
                        return str.Substring(startIndex + 1, i - startIndex - 2);
                    }
                }
                throw new InvalidDataException();
            });
            results.state = parser.ParseNextChar();
            parser.MoveNextOrFail(); // ppid
            parser.MoveNextOrFail(); // pgrp
            results.session = parser.ParseNextInt32();
            parser.MoveNextOrFail(); // tty_nr
            parser.MoveNextOrFail(); // tpgid
            parser.MoveNextOrFail(); // flags
            parser.MoveNextOrFail(); // majflt
            parser.MoveNextOrFail(); // cmagflt
            parser.MoveNextOrFail(); // minflt
            parser.MoveNextOrFail(); // cminflt
            results.utime = parser.ParseNextUInt64();
            results.stime = parser.ParseNextUInt64();
            parser.MoveNextOrFail(); // cutime
            parser.MoveNextOrFail(); // cstime
            parser.MoveNextOrFail(); // priority
            results.nice = parser.ParseNextInt64();
            parser.MoveNextOrFail(); // num_threads
            parser.MoveNextOrFail(); // itrealvalue
            results.starttime = parser.ParseNextUInt64();
            results.vsize = parser.ParseNextUInt64();
            results.rss = parser.ParseNextInt64();
            results.rsslim = parser.ParseNextUInt64();
            parser.MoveNextOrFail(); // startcode
            parser.MoveNextOrFail(); // endcode
            results.startstack = parser.ParseNextUInt64();
            parser.MoveNextOrFail(); // kstkesp
            parser.MoveNextOrFail(); // kstkeip
            parser.MoveNextOrFail(); // signal
            parser.MoveNextOrFail(); // blocked
            parser.MoveNextOrFail(); // sigignore
            parser.MoveNextOrFail(); // sigcatch
            parser.MoveNextOrFail(); // wchan
            parser.MoveNextOrFail(); // nswap
            parser.MoveNextOrFail(); // cnswap
            parser.MoveNextOrFail(); // exit_signal
            parser.MoveNextOrFail(); // processor
            parser.MoveNextOrFail(); // rt_priority
            parser.MoveNextOrFail(); // policy
            parser.MoveNextOrFail(); // delayacct_blkio_ticks
            parser.MoveNextOrFail(); // guest_time
            parser.MoveNextOrFail(); // cguest_time

            return results;
        }
    }
}
