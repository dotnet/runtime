// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        private const string ExeFileName = "/exe";
        private const string CmdLineFileName = "/cmdline";
        private const string StatFileName = "/stat";
        private const string FileDescriptorDirectoryName = "/fd/";
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
            internal int ppid;
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
            //internal ulong startstack;
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

        internal static string GetExeFilePathForProcess(ProcPid pid) =>
            pid == ProcPid.Self ? $"{RootPath}{Self}{ExeFileName}" :
                                  string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{ExeFileName}");

        internal static string GetCmdLinePathForProcess(ProcPid pid) =>
            pid == ProcPid.Self ? $"{RootPath}{Self}{CmdLineFileName}" :
                                  string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{CmdLineFileName}");

        internal static string GetStatFilePathForProcess(ProcPid pid) =>
            pid == ProcPid.Self ? $"{RootPath}{Self}{StatFileName}" :
                                  string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{StatFileName}");

        internal static string GetTaskDirectoryPathForProcess(ProcPid pid) =>
            pid == ProcPid.Self ? $"{RootPath}{Self}{TaskDirectoryName}" :
                                  string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{TaskDirectoryName}");

        internal static string GetFileDescriptorDirectoryPathForProcess(ProcPid pid) =>
            pid == ProcPid.Self ? $"{RootPath}{Self}{FileDescriptorDirectoryName}" :
                                  string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{FileDescriptorDirectoryName}");

        private static string GetStatFilePathForThread(ProcPid pid, int tid) =>
            pid == ProcPid.Self ? string.Create(null, stackalloc char[256], $"{RootPath}{Self}{TaskDirectoryName}{(uint)tid}{StatFileName}") :
                                  string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{TaskDirectoryName}{(uint)tid}{StatFileName}");

        internal static bool TryReadStatFile(ProcPid pid, out ParsedStat result)
        {
            bool b = TryParseStatFile(GetStatFilePathForProcess(pid), out result);
            Debug.Assert(!b || pid == ProcPid.Self|| (ProcPid)result.pid == pid, "Expected process ID from stat file to match supplied pid");
            return b;
        }

        internal static bool TryReadStatFile(ProcPid pid, int tid, out ParsedStat result)
        {
            bool b = TryParseStatFile(GetStatFilePathForThread(pid, tid), out result);
            Debug.Assert(!b || result.pid == tid, "Expected thread ID from stat file to match supplied tid");
            return b;
        }

        internal static bool TryParseStatFile(string statFilePath, out ParsedStat result)
        {
            if (!TryReadFile(statFilePath, out string? statFileContents))
            {
                // Between the time that we get an ID and the time that we try to read the associated stat
                // file(s), the process could be gone.
                result = default(ParsedStat);
                return false;
            }

            var parser = new StringParser(statFileContents, ' ');
            var results = default(ParsedStat);

            results.pid = parser.ParseNextInt32();
            results.comm = parser.MoveAndExtractNextInOuterParens();
            results.state = parser.ParseNextChar();
            results.ppid = parser.ParseNextInt32();
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

            // The following lines are commented out as there's no need to parse through
            // the rest of the entry (we've gotten all of the data we need).  Should any
            // of these fields be needed in the future, uncomment all of the lines up
            // through and including the one that's needed.  For now, these are being left
            // commented to document what's available in the remainder of the entry.

            //parser.MoveNextOrFail(); // startcode
            //parser.MoveNextOrFail(); // endcode
            //parser.MoveNextOrFail(); // startstack
            //parser.MoveNextOrFail(); // kstkesp
            //parser.MoveNextOrFail(); // kstkeip
            //parser.MoveNextOrFail(); // signal
            //parser.MoveNextOrFail(); // blocked
            //parser.MoveNextOrFail(); // sigignore
            //parser.MoveNextOrFail(); // sigcatch
            //parser.MoveNextOrFail(); // wchan
            //parser.MoveNextOrFail(); // nswap
            //parser.MoveNextOrFail(); // cnswap
            //parser.MoveNextOrFail(); // exit_signal
            //parser.MoveNextOrFail(); // processor
            //parser.MoveNextOrFail(); // rt_priority
            //parser.MoveNextOrFail(); // policy
            //parser.MoveNextOrFail(); // delayacct_blkio_ticks
            //parser.MoveNextOrFail(); // guest_time
            //parser.MoveNextOrFail(); // cguest_time

            result = results;
            return true;
        }
    }
}
