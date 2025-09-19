using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public sealed partial class GenericsTests : LinkerTestBase
    {
        protected override string TestSuiteName => "Generics";

        [Fact]
        public Task InstantiatedGenericEquality()
        {
            return RunTest();
        }

        [Fact]
        public Task GenericConstraints()
        {
            return RunTest();
        }

    }
}
