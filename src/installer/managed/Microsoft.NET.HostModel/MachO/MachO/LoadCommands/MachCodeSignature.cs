// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed class MachCodeSignature : MachLinkEdit
    {
        public MachCodeSignature(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachCodeSignature(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}
