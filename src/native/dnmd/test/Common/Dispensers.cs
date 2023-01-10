//#define NETFX_20_BASELINE
//#define NETFX_40_BASELINE

using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
        /// Directory for NET Framework 2.0
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static string NetFx20Dir { get; } = GetNetFx20Install();

        /// <summary>
        /// Directory for NET Framework 4.0
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static string NetFx40Dir { get; } = GetNetFx40Install();

        /// <summary>
        /// Get the current IMetaDataDispenser implementation.
        /// </summary>
        public static IMetaDataDispenser Current { get; } = GetCurrent();

        private static unsafe nint GetBaselineRaw()
        {
#if NETFX_20_BASELINE
            var baseline = Path.Combine(GetNetFx20Install(), "mscorwks.dll");
#elif NETFX_40_BASELINE
            var baseline = Path.Combine(GetNetFx40Install(), "clr.dll");
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

        private static unsafe IMetaDataDispenser GetCurrent()
        {
            var runtimeName =
                OperatingSystem.IsWindows() ? "dnmd_interfaces.dll"
                : OperatingSystem.IsMacOS() ? "libdnmd_interfaces.dylib"
                : "libdnmd_interfaces.so";

            nint mod = NativeLibrary.Load(runtimeName);
            var getter = (delegate* unmanaged<in Guid, out IMetaDataDispenser, int>)NativeLibrary.GetExport(mod, "GetDispenser");

            Guid iid = typeof(IMetaDataDispenser).GUID;
            int result = getter(in iid, out IMetaDataDispenser dispenser);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            return dispenser;
        }

        [SupportedOSPlatform("windows")]
        private static string GetNetFx20Install()
        {
            using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\.NETFramework")!;
            return Path.Combine((string)key.GetValue("InstallRoot")!, "v2.0.50727");
        }

        [SupportedOSPlatform("windows")]
        private static string GetNetFx40Install()
        {
            using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\.NETFramework")!;
            return Path.Combine((string)key.GetValue("InstallRoot")!, "v4.0.30319");
        }
    }
}
