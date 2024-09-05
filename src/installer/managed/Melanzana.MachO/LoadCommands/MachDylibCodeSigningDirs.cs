// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public class MachDylibCodeSigningDirs : MachLinkEdit
    {
        public MachDylibCodeSigningDirs(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDylibCodeSigningDirs(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}
