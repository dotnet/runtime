// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public partial class MachUuid : MachLoadCommand
    {
        public Guid Uuid { get; set; }
    }
}
