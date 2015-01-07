// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    internal sealed class ProcessThreadTimes
    {
        internal long create, exit, kernel, user;

        public DateTime StartTime { get { return DateTime.FromFileTime(create); } }
        public DateTime ExitTime { get { return DateTime.FromFileTime(exit); } }
        public TimeSpan PrivilegedProcessorTime { get { return new TimeSpan(kernel); } }
        public TimeSpan UserProcessorTime { get { return new TimeSpan(user); } }
        public TimeSpan TotalProcessorTime { get { return new TimeSpan(user + kernel); } }
    }
}
