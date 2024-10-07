// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed class MachRunPath : MachLoadCommand
    {
        public string RunPath { get; set; } = string.Empty;
    }
}
