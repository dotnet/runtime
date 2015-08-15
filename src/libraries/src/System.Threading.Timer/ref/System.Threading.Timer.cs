// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Threading
{
    public sealed partial class Timer : System.IDisposable
    {
        public Timer(System.Threading.TimerCallback callback, object state, int dueTime, int period) { }
        public Timer(System.Threading.TimerCallback callback, object state, System.TimeSpan dueTime, System.TimeSpan period) { }
        public bool Change(int dueTime, int period) { return default(bool); }
        public bool Change(System.TimeSpan dueTime, System.TimeSpan period) { return default(bool); }
        public void Dispose() { }
    }
}
