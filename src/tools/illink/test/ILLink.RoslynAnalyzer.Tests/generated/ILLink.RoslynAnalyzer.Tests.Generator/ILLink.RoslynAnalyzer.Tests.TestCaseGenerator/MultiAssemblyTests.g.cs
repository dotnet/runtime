using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public sealed partial class MultiAssemblyTests : LinkerTestBase
    {

        protected override string TestSuiteName => "MultiAssembly";

        [Fact]
        public Task ForwarderReference()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task MultiAssembly()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task TypeRefToAssembly()
        {
            return RunTest(allowMissingWarnings: true);
        }

    }
}
