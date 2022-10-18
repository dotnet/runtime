namespace Melanzana.MachO
{
    public class MachLayoutOptions
    {
        public MachLayoutOptions(MachObjectFile objectFile)
        {
            if (objectFile.FileType == MachFileType.Object)
            {
                // Unlinked object
                BaseAddress = 0;
                // There's single segment, so no alignment is necessary
                SegmentAlignment = 1;
            }
            else
            {
                // Presumably there's a __PAGEZERO section at zero address
                BaseAddress = 0;

                SegmentAlignment = objectFile.CpuType switch
                {
                    MachCpuType.Arm64 => 0x4000,
                    MachCpuType.Arm => 0x4000,
                    _ => 0x1000,
                };
            }
        }

        public uint BaseAddress { get; set; }

        public uint SegmentAlignment { get; set; }
    }
}
