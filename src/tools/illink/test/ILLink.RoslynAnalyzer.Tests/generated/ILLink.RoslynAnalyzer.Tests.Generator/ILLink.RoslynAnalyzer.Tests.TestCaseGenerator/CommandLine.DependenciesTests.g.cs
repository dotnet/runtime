using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.CommandLine
{
    public sealed partial class DependenciesTests : LinkerTestBase
    {

        protected override string TestSuiteName => "CommandLine.Dependencies";

        [Fact]
        public Task MultipleEntryPointRoots_Lib()
        {
            return RunTest(allowMissingWarnings: true);
        }

    }
}
