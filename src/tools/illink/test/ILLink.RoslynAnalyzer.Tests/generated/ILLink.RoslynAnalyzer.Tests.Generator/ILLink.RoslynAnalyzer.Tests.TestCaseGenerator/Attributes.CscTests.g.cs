using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Attributes
{
	public sealed partial class CscTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Attributes.Csc";

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeArrayOnAttributeCtorOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnAssemblyOtherTypesInAttributeAssemblyUsed ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnEvent ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnProperty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeCtorOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeFieldOnAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeFieldOnEvent ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeFieldOnMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeFieldOnProperty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributeFieldOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributePropertyOnAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributePropertyOnEvent ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributePropertyOnMethod ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributePropertyOnProperty ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyIsTypeOnAttributePropertyOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task OnlyTypeUsedInAssemblyWithReferenceIsTypeOnAttributeCtorOnType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}