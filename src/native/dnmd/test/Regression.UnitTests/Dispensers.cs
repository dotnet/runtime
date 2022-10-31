
using System.Runtime.InteropServices;

namespace Regression.UnitTests
{
    internal static class Dispensers
    {
        /// <summary>
        /// Get the baseline IMetaDataDispenser implementation.
        /// </summary>
        public static IMetaDataDispenser Baseline { get; } = GetBaseline();

        /// <summary>
        /// Get the current IMetaDataDispenser implementation.
        /// </summary>
        public static IMetaDataDispenser Current { get; } = GetCurrent();

        private static unsafe IMetaDataDispenser GetBaseline()
        {
            var runtimeName =
                OperatingSystem.IsWindows() ? "coreclr.dll"
                : OperatingSystem.IsMacOS() ? "libcorclr.dylib"
                : "libcoreclr.so";

            var baseline = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, runtimeName);

            nint mod = NativeLibrary.Load(baseline);
            var getter = (delegate* unmanaged<in Guid, in Guid, out IMetaDataDispenser, int>)NativeLibrary.GetExport(mod, "MetaDataGetDispenser");

            Guid clsid = new("E5CB7A31-7512-11D2-89CE-0080C792E5D8");
            Guid iid = typeof(IMetaDataDispenser).GUID;
            int result = getter(in clsid, in iid, out IMetaDataDispenser dispenser);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            return dispenser;
        }

        private static unsafe IMetaDataDispenser GetCurrent()
        {
            var runtimeName =
                OperatingSystem.IsWindows() ? "dnmd_interfaces.dll"
                : OperatingSystem.IsMacOS() ? "libdnmd_interfaces.dylib"
                : "libdnmd_interfaces.so";

            var current = Path.Combine(@"Debug", runtimeName);

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
