using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.CommandLine.Mvid
{
    public sealed partial class IndividualTests : LinkerTestBase
    {

        protected override string TestSuiteName => "CommandLine.Mvid.Individual";

        [Fact]
        public Task DeterministicMvidWorks()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task NewMvidWorks()
        {
            return RunTest(allowMissingWarnings: true);
        }

        [Fact]
        public Task RetainMvid()
        {
            return RunTest(allowMissingWarnings: true);
        }

    }
}
