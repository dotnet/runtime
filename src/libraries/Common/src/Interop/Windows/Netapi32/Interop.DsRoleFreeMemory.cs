internal static partial class Interop
{
    internal static partial class Netapi32
    {
        [DllImport("Netapi32.dll")]
        public static extern int DsRoleFreeMemory([In] IntPtr buffer);
    }
}
