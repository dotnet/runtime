using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32.RegistryTests
{
    internal static class Helpers
    {
        [DllImport("api-ms-win-core-registry-l2-1-0.dll", CharSet = CharSet.Unicode, EntryPoint = "RegSetValueW", SetLastError = true)]
        private static extern int RegSetValue(SafeRegistryHandle handle, string value, int regType, string sb, int sizeIgnored);

        internal const string _DefaultValue = "default";

        internal static bool SetDefaultValue(this RegistryKey key)
        {
            const int REG_SZ = 1;
            return RegSetValue(key.Handle, null, REG_SZ, _DefaultValue, 0) == 0;
        }

        [DllImport("api-ms-win-core-registry-l1-1-0.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        private static extern int RegQueryValueEx(SafeRegistryHandle handle, string valueName, int[] reserved, IntPtr regType, [Out] byte[] value, ref int size);

        internal static bool IsDefaultValueSet(this RegistryKey key)
        {
            const int ERROR_FILE_NOT_FOUND = 2;
            byte[] b = new byte[4];
            int size = 4;
            return RegQueryValueEx(key.Handle, null, null, IntPtr.Zero, b, ref size) != ERROR_FILE_NOT_FOUND;
        }
    }

}
