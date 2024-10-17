// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed class MachDyldExportsTrie : MachLinkEdit
    {
        public MachDyldExportsTrie(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDyldExportsTrie(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}
