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