// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess.Tests
{
    [Flags]
    public enum PipeMessageByteCode
    {
        Start = 0,
        Continue = 1,
        Pause = 2,
        Stop = 4,
        OnCustomCommand = 8,
        ExceptionThrown = 16,
        Connected = 32
    };
}
