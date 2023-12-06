using System.Runtime.InteropServices;

using Common;
using Xunit;
using Xunit.Abstractions;

namespace Regression.UnitTests
{
    public unsafe class SymReaderTests
    {
        private delegate* unmanaged<void*, int, TestResult> _symReaderAPIs;

        public SymReaderTests(ITestOutputHelper outputHelper)
        {
            Log = outputHelper;
            nint mod = NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, Native.Path));
            _symReaderAPIs = (delegate* unmanaged<void*, int, TestResult>)NativeLibrary.GetExport(mod, "UnitSymReaderAPIs");
        }

        private ITestOutputHelper Log { get; }

        [Fact]
        public void SymReaderAPIs() => _symReaderAPIs(null, 0).Check();
    }
}