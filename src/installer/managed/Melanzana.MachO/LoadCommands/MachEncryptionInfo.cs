namespace Melanzana.MachO
{
    [GenerateReaderWriter]
    public partial class MachEncryptionInfo : MachLoadCommand
    {
        public uint CryptOffset;
        public uint CryptSize;
        public uint CryptId;
    }
}
