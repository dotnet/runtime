using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
    [SetupCSharpCompilerToUse("csc")]
    [SetupCompileArgument("/optimize+")]
    [SetupLinkerArgument("--enable-opt", "ipconstprop")]
    public class OSPlatformGuard
    {
        public static void Main()
        {
            TestRuntimeInformationIsOSPlatform();
            TestRuntimeInformationCreate();
            TestOperatingSystemIsOSPlatform();

            // Keep every branch target reachable independently so their retention is platform-independent.
            // The guards above are still folded to a constant, asserted via ExpectBodyModified (without the
            // platform-guard folding IsOSPlatform never folds and the bodies would be unchanged).
            Windows();
            NotWindows();
            Linux();
            NotLinux();
            FreeBSD();
            NotFreeBSD();
        }

        [Kept]
        [ExpectBodyModified]
        static void TestRuntimeInformationIsOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Windows();
            else
                NotWindows();
        }

        [Kept]
        [ExpectBodyModified]
        static void TestRuntimeInformationCreate()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
                FreeBSD();
            else
                NotFreeBSD();
        }

        [Kept]
        [ExpectBodyModified]
        static void TestOperatingSystemIsOSPlatform()
        {
            if (System.OperatingSystem.IsOSPlatform("Linux"))
                Linux();
            else
                NotLinux();
        }

        [Kept]
        static void Windows()
        {
        }

        [Kept]
        static void NotWindows()
        {
        }

        [Kept]
        static void Linux()
        {
        }

        [Kept]
        static void NotLinux()
        {
        }

        [Kept]
        static void FreeBSD()
        {
        }

        [Kept]
        static void NotFreeBSD()
        {
        }
    }
}
