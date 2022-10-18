using System.Diagnostics;

namespace Melanzana.MachO
{
    public class MachTwoLevelHints : MachLoadCommand
    {
        public MachTwoLevelHints(MachObjectFile objectFile, MachLinkEditData data)
        {
            Data = data;
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