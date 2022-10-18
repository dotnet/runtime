namespace Melanzana.MachO
{
    public abstract class MachLoadCommand
    {
        protected MachLoadCommand()
        {
        }

        internal virtual IEnumerable<MachLinkEditData> LinkEditData => Array.Empty<MachLinkEditData>();
    }
}