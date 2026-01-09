using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Reflection
{
    public sealed partial class DependenciesTests : LinkerTestBase
    {

        protected override string TestSuiteName => "Reflection.Dependencies";

        [Fact]
        public Task TypeMapReferencedAssembly()
        {
            return RunTest(allowMissingWarnings: true);
        }

    }
}
