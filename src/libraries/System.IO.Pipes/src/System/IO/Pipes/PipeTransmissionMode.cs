// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.IO.Pipes
{
    public enum PipeTransmissionMode
    {
        Byte = 0,
        [MinimumOSPlatform("windows7.0")]
        Message = 1,
    }
}
