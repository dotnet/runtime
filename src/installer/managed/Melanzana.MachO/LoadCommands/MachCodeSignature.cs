namespace Melanzana.MachO
{
    public class MachCodeSignature : MachLinkEdit
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