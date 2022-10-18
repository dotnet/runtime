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