// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public class MachSegmentSplitInfo : MachLinkEdit
    {
        public MachSegmentSplitInfo(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachSegmentSplitInfo(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}
