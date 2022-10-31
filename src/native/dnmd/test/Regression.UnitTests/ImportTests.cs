using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using Xunit;
using Xunit.Abstractions;

namespace Regression.UnitTests
{
    public unsafe class ImportTests
    {
        public ImportTests(ITestOutputHelper outputHelper)
        {
            Log = outputHelper;
        }

        private ITestOutputHelper Log { get; }

        public static IEnumerable<object[]> FrameworkLibraries()
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

        [Theory]
        [MemberData(nameof(FrameworkLibraries))]
        public void LoadMetadata(string filename, PEReader managedLibrary)
        {
            Log.WriteLine($"Loading {filename}...");

            using var _ = managedLibrary;
            PEMemoryBlock block = managedLibrary.GetMetadata();

            var flags = CorOpenFlags.ReadOnly;
            var iid = typeof(IMetaDataImport).GUID;

            var impls = new[]
                {
                    Dispensers.Baseline,
                    Dispensers.Current
                };
            foreach (IMetaDataDispenser disp in impls)
            {
                void* pUnk;
                int hr = disp.OpenScopeOnMemory(block.Pointer, block.Length, flags, &iid, &pUnk);
                Assert.Equal(0, hr);
                Assert.NotEqual(0, (nint)pUnk);
                Marshal.Release((nint)pUnk);
            }
        }
    }
}