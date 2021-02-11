// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed class AutoResetEvent : EventWaitHandle
    {
        public AutoResetEvent(bool initialState) : base(initialState, EventResetMode.AutoReset) { }
    }
}
