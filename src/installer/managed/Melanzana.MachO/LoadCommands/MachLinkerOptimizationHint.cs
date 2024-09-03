namespace Melanzana.MachO
{
    public class MachLinkerOptimizationHint : MachLinkEdit
    {
        public MachLinkerOptimizationHint(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachLinkerOptimizationHint(MachObjectFile objectFile, MachLinkEditData data)
            : base(objectFile, data)
        {
        }
    }
}