// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public class MachDyldChainedFixups : MachLinkEdit
    {
        public MachDyldChainedFixups(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDyldChainedFixups(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}