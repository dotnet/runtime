// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public class MachFunctionStarts : MachLinkEdit
    {
        public MachFunctionStarts(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachFunctionStarts(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}
