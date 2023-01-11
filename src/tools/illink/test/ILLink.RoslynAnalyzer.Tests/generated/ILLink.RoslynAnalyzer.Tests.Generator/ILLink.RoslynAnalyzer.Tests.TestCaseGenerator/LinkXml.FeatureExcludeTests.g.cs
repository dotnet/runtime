using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.LinkXml
{
	public sealed partial class FeatureExcludeTests : LinkerTestBase
	{

		protected override string TestSuiteName => "LinkXml.FeatureExclude";

		[Fact]
		public Task OnAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnEvent ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnField ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnProperty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}