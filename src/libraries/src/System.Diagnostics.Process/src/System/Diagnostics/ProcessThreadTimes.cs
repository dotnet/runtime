// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    internal sealed class ProcessThreadTimes
    {
        internal long _create, _exit, _kernel, _user;

        public DateTime StartTime { get { return DateTime.FromFileTime(_create); } }
        public DateTime ExitTime { get { return DateTime.FromFileTime(_exit); } }
        public TimeSpan PrivilegedProcessorTime { get { return new TimeSpan(_kernel); } }
        public TimeSpan UserProcessorTime { get { return new TimeSpan(_user); } }
        public TimeSpan TotalProcessorTime { get { return new TimeSpan(_user + _kernel); } }
    }
}
