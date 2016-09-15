// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Timers
{
    public class ElapsedEventArgs : EventArgs
    {
        private readonly DateTime _signalTime;

        internal ElapsedEventArgs(long fileTime)
        {
            _signalTime = DateTime.FromFileTime(fileTime);
        }

        public DateTime SignalTime
        {
            get
            {
                return _signalTime;
            }
        }
    }
}

