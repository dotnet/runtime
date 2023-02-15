using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class LinqExpressionsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "LinqExpressions";

		[Fact]
		public Task CanDisableOperatorDiscovery ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanPreserveCustomOperators ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanPreserveNullableCustomOperators ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanRemoveMethodsNamedLikeCustomOperators ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CanRemoveOperatorsWhenNotUsingLinqExpressions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task CustomOperatorsWithUnusedArgumentTypes ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}