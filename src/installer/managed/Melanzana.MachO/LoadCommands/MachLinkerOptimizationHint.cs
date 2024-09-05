// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public class MachLinkerOptimizationHint : MachLinkEdit
    {
        public MachLinkerOptimizationHint(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachLinkerOptimizationHint(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}
