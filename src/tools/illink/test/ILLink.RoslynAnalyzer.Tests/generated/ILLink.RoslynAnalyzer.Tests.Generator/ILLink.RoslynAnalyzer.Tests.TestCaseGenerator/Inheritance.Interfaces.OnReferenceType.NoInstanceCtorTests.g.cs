using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces.OnReferenceType
{
	public sealed partial class NoInstanceCtorTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.OnReferenceType.NoInstanceCtor";

		[Fact]
		public Task NoInstanceCtorAndAssemblyPreserveAll ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoInstanceCtorAndTypePreserveAll ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoInstanceCtorAndTypePreserveFields ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoInstanceCtorAndTypePreserveFieldsWithInterfacesMarked ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoInstanceCtorAndTypePreserveMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoInstanceCtorAndTypePreserveMethodsWithInterfacesMarked ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NoInstanceCtorAndTypePreserveNone ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}