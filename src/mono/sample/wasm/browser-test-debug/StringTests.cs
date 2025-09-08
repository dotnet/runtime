using System.Runtime.InteropServices;

namespace Sample
{
    public class StringTests
    {
        [DllImport("wasmString", EntryPoint = "printString")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        public static extern void PrintMyStringFromCSharp([MarshalAs(UnmanagedType.LPStr)] string _filePath);
    }
}
