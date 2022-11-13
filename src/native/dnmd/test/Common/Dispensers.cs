//#define NETFX_20_BASELINE
//#define NETFX_40_BASELINE

#if NETFX_20_BASELINE || NETFX_40_BASELINE
using Microsoft.Win32;
#endif

using System.Runtime.InteropServices;

namespace Common
{
    internal static class Dispensers
    {
        /// <summary>
        /// Get the baseline IMetaDataDispenser implementation.
        /// </summary>
        public static IMetaDataDispenser Baseline { get; } = GetBaseline();

        /// <summary>
        /// Get the baseline IMetaDataDispenser implementation as a pointer.
        /// </summary>
        public static nint BaselineRaw { get; } = GetBaselineRaw();

        /// <summary>
        /// Get the current IMetaDataDispenser implementation.
        /// </summary>
        public static IMetaDataDispenser Current { get; } = GetCurrent();

        private static unsafe nint GetBaselineRaw()
        {
#if NETFX_20_BASELINE || NETFX_40_BASELINE
            using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\.NETFramework");
#if NETFX_20_BASELINE
            var baseline = Path.Combine((string)key.GetValue("InstallRoot")!, "v2.0.50727", "mscorwks.dll");
#else // NETFX_40_BASELINE
            var baseline = Path.Combine((string)key.GetValue("InstallRoot")!, "v4.0.30319", "clr.dll");
#endif
#else
            var runtimeName =
                OperatingSystem.IsWindows() ? "coreclr.dll"
                : OperatingSystem.IsMacOS() ? "libcorclr.dylib"
                : "libcoreclr.so";

            var baseline = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, runtimeName);
#endif

            nint mod = NativeLibrary.Load(baseline);
            var getter = (delegate* unmanaged<in Guid, in Guid, out nint, int>)NativeLibrary.GetExport(mod, "MetaDataGetDispenser");

            Guid clsid = new("E5CB7A31-7512-11D2-89CE-0080C792E5D8");
            Guid iid = typeof(IMetaDataDispenser).GUID;
            int result = getter(in clsid, in iid, out nint dispenser);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            return dispenser;
        }

        private static unsafe IMetaDataDispenser GetBaseline()
        {
            nint ptr = GetBaselineRaw();
            var dispenser = (IMetaDataDispenser)Marshal.GetObjectForIUnknown(ptr);
            _ = Marshal.Release(ptr);
            return dispenser;
        }

        private static string GetCurrentDirectory()
        {
            return @"";
        }

        private static unsafe IMetaDataDispenser GetCurrent()
        {
            var runtimeName =
                OperatingSystem.IsWindows() ? "dnmd_interfaces.dll"
                : OperatingSystem.IsMacOS() ? "libdnmd_interfaces.dylib"
                : "libdnmd_interfaces.so";

            var current = Path.Combine(GetCurrentDirectory(), runtimeName);

            nint mod = NativeLibrary.Load(current);
            var getter = (delegate* unmanaged<in Guid, out IMetaDataDispenser, int>)NativeLibrary.GetExport(mod, "GetDispenser");

            Guid iid = typeof(IMetaDataDispenser).GUID;
            int result = getter(in iid, out IMetaDataDispenser dispenser);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            return dispenser;
        }
    }
}
