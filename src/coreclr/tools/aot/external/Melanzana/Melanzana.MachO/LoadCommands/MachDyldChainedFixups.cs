namespace Melanzana.MachO
{
    public class MachDyldChainedFixups : MachLinkEdit
    {
        public MachDyldChainedFixups(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDyldChainedFixups(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}