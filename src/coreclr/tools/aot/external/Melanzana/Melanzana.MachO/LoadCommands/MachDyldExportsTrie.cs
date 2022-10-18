namespace Melanzana.MachO
{
    public class MachDyldExportsTrie : MachLinkEdit
    {
        public MachDyldExportsTrie(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDyldExportsTrie(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}