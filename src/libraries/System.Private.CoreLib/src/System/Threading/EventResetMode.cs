// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    /// <summary>
    /// Indicates whether an <see cref="EventWaitHandle" /> is reset automatically or manually after receiving a signal.
    /// </summary>
    public enum EventResetMode
    {
        AutoReset = 0,
        ManualReset = 1
    }
}
