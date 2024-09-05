// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public abstract class MachLinkEdit : MachLoadCommand
    {
        protected readonly MachObjectFile objectFile;

        protected MachLinkEdit(MachObjectFile objectFile)
        {
            if (objectFile is null) throw new ArgumentNullException(nameof(objectFile));

            Data = new MachLinkEditData();
            this.objectFile = objectFile;
        }

        protected MachLinkEdit(MachObjectFile objectFile, MachLinkEditData data)
        {
            if (objectFile is null) throw new ArgumentNullException(nameof(objectFile));
            if (data is null) throw new ArgumentNullException(nameof(data));

            Data = data;
            this.objectFile = objectFile;
        }

        public uint FileOffset => Data.FileOffset;

        public uint FileSize => (uint)Data.Size;

        public MachLinkEditData Data { get; private init; }

        internal override IEnumerable<MachLinkEditData> LinkEditData
        {
            get
            {
                yield return Data;
            }
        }
    }
}
