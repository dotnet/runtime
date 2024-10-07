// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed partial class MachUuid : MachLoadCommand
    {
        public Guid Uuid { get; set; }
    }
}
