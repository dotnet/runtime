namespace Melanzana.MachO
{
    public class MachDataInCode : MachLinkEdit
    {
        public MachDataInCode(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDataInCode(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}