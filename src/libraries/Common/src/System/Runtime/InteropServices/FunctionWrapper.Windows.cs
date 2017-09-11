namespace System.Runtime.InteropServices
{
    internal static partial class FunctionWrapper
    {
        private static IntPtr LoadFunctionPointer(IntPtr nativeLibraryHandle, string functionName) => Interop.Kernel32.GetProcAddress(nativeLibraryHandle, functionName);
    }
}
