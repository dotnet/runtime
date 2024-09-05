// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public abstract class MachLoadCommand
    {
        protected MachLoadCommand()
        {
        }

        internal virtual IEnumerable<MachLinkEditData> LinkEditData => Array.Empty<MachLinkEditData>();
    }
}
