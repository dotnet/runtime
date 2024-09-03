namespace Melanzana.MachO
{
    public class MachDyldInfoOnly : MachDyldInfo
    {
        public MachDyldInfoOnly(MachObjectFile objectFile)
            : base(objectFile)
        {
        }

        public MachDyldInfoOnly(
            MachObjectFile objectFile,
            MachLinkEditData rebaseData,
            MachLinkEditData bindData,
            MachLinkEditData weakBindData,
            MachLinkEditData lazyBindData,
            MachLinkEditData exportData)
            : base(objectFile, rebaseData, bindData, weakBindData, lazyBindData, exportData)
        {
        }
    }
}