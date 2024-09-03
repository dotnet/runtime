using System.Text;
using Melanzana.MachO.BinaryFormat;

namespace Melanzana.MachO
{
    public class MachEntrypointCommand : MachLoadCommand
    {
        public ulong FileOffset { get; set; }

        public ulong StackSize { get; set; }
    }
}
