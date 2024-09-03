namespace Melanzana.MachO
{
    public class MachDylibCodeSigningDirs : MachLinkEdit
    {
        public MachDylibCodeSigningDirs(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDylibCodeSigningDirs(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}