using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Reflection
{
    public sealed partial class IndividualTests : LinkerTestBase
    {

        protected override string TestSuiteName => "Reflection.Individual";

        [Fact]
        public Task CanOutputTypeMaps()
        {
            return RunTest(allowMissingWarnings: true);
        }

    }
}
