// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static partial class Signals
        {
            internal const int SIGHUP = 1;
            internal const int SIGINT = 2;
            internal const int SIGQUIT = 3;
            internal const int SIGILL = 4;
            internal const int SIGTRAP = 5;
            internal const int SIGABRT = 6;
            internal const int SIGIOT = 6;
            internal const int SIGBUS = 7;
            internal const int SIGFPE = 8;
            internal const int SIGKILL = 9;
            internal const int SIGUSR1 = 10;
            internal const int SIGSEGV = 11;
            internal const int SIGUSR2 = 12;
            internal const int SIGPIPE = 13;
            internal const int SIGALRM = 14;
            internal const int SIGTERM = 15;
            internal const int SIGSTKFLT = 16;
            internal const int SIGCLD = SIGCHLD;
            internal const int SIGCHLD = 17;
            internal const int SIGCONT = 18;
            internal const int SIGSTOP = 19;
            internal const int SIGTSTP = 20;
            internal const int SIGTTIN = 21;
            internal const int SIGTTOU = 22;
            internal const int SIGURG = 23;
            internal const int SIGXCPU = 24;
            internal const int SIGXFSZ = 25;
            internal const int SIGVTALRM = 26;
            internal const int SIGPROF = 27;
            internal const int SIGWINCH = 28;
            internal const int SIGIO = 29;
            internal const int SIGPOLL = SIGIO;
            internal const int SIGPWR = 30;
            internal const int SIGSYS = 31;
            internal const int SIGUNUSED = 31;
        }
    }
}