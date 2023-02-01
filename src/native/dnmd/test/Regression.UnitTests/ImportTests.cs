using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Xunit;
using Xunit.Abstractions;

using Common;
using System.Text;

namespace Regression.UnitTests
{
    public sealed class WindowsOnlyTheoryAttribute : TheoryAttribute
    {
        public WindowsOnlyTheoryAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Only run on Windows";
            }
        }
    }

    public unsafe class ImportTests
    {
        private delegate* unmanaged<void*, int, TestResult> _importAPIs;
        private delegate* unmanaged<void*, int, TestResult> _longRunningAPIs;
        private delegate* unmanaged<void*, int, TestResult> _findAPIs;

        public ImportTests(ITestOutputHelper outputHelper)
        {
            Log = outputHelper;

            string regnativePath =
                OperatingSystem.IsWindows() ? "regnative.dll"
                : OperatingSystem.IsMacOS() ? "libregnative.dylib"
                : "libregnative.so";

            nint mod = NativeLibrary.Load(regnativePath);
            var initialize = (delegate* unmanaged<void*, int>)NativeLibrary.GetExport(mod, "UnitInitialize");
            int hr = initialize((void*)Dispensers.Baseline);
            if (hr < 0)
            {
                throw new Exception($"Initialization failed: 0x{hr:x}");
            }

            _importAPIs = (delegate* unmanaged<void*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitImportAPIs");
            _longRunningAPIs = (delegate* unmanaged<void*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitLongRunningAPIs");
            _findAPIs = (delegate* unmanaged<void*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitFindAPIs");
        }

        private ITestOutputHelper Log { get; }

        public static IEnumerable<object[]> CoreFrameworkLibraries()
        {
            var spcl = typeof(object).Assembly.Location;
            var frameworkDir = Path.GetDirectoryName(spcl)!;
            foreach (var managedMaybe in Directory.EnumerateFiles(frameworkDir, "*.dll"))
            {
                PEReader pe = new(File.OpenRead(managedMaybe));
                if (!pe.HasMetadata)
                {
                    pe.Dispose();
                    continue;
                }

                yield return new object[] { Path.GetFileName(managedMaybe), pe };
            }
        }

        [SupportedOSPlatform("windows")]
        public static IEnumerable<object[]> Net20FrameworkLibraries()
        {
            foreach (var managedMaybe in Directory.EnumerateFiles(Dispensers.NetFx20Dir, "*.dll"))
            {
                PEReader pe = new(File.OpenRead(managedMaybe));
                if (!pe.HasMetadata)
                {
                    pe.Dispose();
                    continue;
                }

                yield return new object[] { Path.GetFileName(managedMaybe), pe };
            }
        }

        [SupportedOSPlatform("windows")]
        public static IEnumerable<object[]> Net40FrameworkLibraries()
        {
            foreach (var managedMaybe in Directory.EnumerateFiles(Dispensers.NetFx40Dir, "*.dll"))
            {
                PEReader pe = new(File.OpenRead(managedMaybe));
                if (!pe.HasMetadata)
                {
                    pe.Dispose();
                    continue;
                }

                yield return new object[] { Path.GetFileName(managedMaybe), pe };
            }
        }

        public static IEnumerable<object[]> AllCoreLibs()
        {
            List<string> corelibs = new() { typeof(object).Assembly.Location };

            if (OperatingSystem.IsWindows())
            {
                corelibs.Add(Path.Combine(Dispensers.NetFx20Dir, "mscorlib.dll"));
                corelibs.Add(Path.Combine(Dispensers.NetFx40Dir, "mscorlib.dll"));
            }

            foreach (var corelibMaybe in corelibs)
            {
                if (!File.Exists(corelibMaybe))
                {
                    continue;
                }

                PEReader pe = new(File.OpenRead(corelibMaybe));
                yield return new object[] { Path.GetFileName(corelibMaybe), pe };
            }
        }

        [Theory]
        [MemberData(nameof(CoreFrameworkLibraries))]
        public void ImportAPIs_Core(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        [WindowsOnlyTheory]
        [MemberData(nameof(Net20FrameworkLibraries))]
        public void ImportAPIs_Net20(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        [WindowsOnlyTheory]
        [MemberData(nameof(Net40FrameworkLibraries))]
        public void ImportAPIs_Net40(string filename, PEReader managedLibrary) => ImportAPIs(filename, managedLibrary);

        private enum TestState
        {
            Fail,
            Pass
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct TestResult
        {
            public TestState State;
            public byte* FailureMessage;
            public delegate* unmanaged<void*, void> Free;

            public void Check()
            {
                if (State != TestState.Pass)
                {
                    Assert.True(FailureMessage != null);
                    Assert.True(Free != null);
                    string msg = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(FailureMessage));
                    Free(FailureMessage);
                    Assert.Fail(msg);
                }
            }
        }

        private void ImportAPIs(string filename, PEReader managedLibrary)
        {
            Debug.WriteLine($"{nameof(ImportAPIs)} - {filename}");
            using var _ = managedLibrary;
            PEMemoryBlock block = managedLibrary.GetMetadata();

            _importAPIs(block.Pointer, block.Length).Check();
        }

        /// <summary>
        /// These APIs are very expensive to run on all managed libraries. This library only runs
        /// them on the system corelibs and only on a reduced selection of the tokens.
        /// </summary>
        [Theory]
        [MemberData(nameof(AllCoreLibs))]
        public void LongRunningAPIs(string filename, PEReader managedLibrary)
        {
            Debug.WriteLine($"{nameof(LongRunningAPIs)} - {filename}");
            using var _lib = managedLibrary;
            PEMemoryBlock block = managedLibrary.GetMetadata();

            _longRunningAPIs(block.Pointer, block.Length).Check();
        }

        [Fact]
        public void FindAPIs()
        {
            var dir = Path.GetDirectoryName(typeof(ImportTests).Assembly.Location)!;
            var tgtAssembly = Path.Combine(dir, "Regression.TargetAssembly.dll");
            using PEReader managedLibrary = new(File.OpenRead(tgtAssembly));
            PEMemoryBlock block = managedLibrary.GetMetadata();

            _findAPIs(block.Pointer, block.Length).Check();
        }
    }
}