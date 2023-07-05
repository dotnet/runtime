using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/CanVerifyInterfacesOnTypesInAssembly_Lib.cs" })]
	[KeptInterfaceOnTypeInAssembly ("library", typeof (CanVerifyInterfacesOnTypesInAssembly_Lib.A), "library", typeof (CanVerifyInterfacesOnTypesInAssembly_Lib.IFoo))]
	[RemovedInterfaceOnTypeInAssembly ("library", typeof (CanVerifyInterfacesOnTypesInAssembly_Lib.A), "library", typeof (CanVerifyInterfacesOnTypesInAssembly_Lib.IBar))]
	public class CanVerifyInterfacesOnTypesInAssembly
	{
		public static void Main ()
		{
			CanVerifyInterfacesOnTypesInAssembly_Lib.IFoo a = new CanVerifyInterfacesOnTypesInAssembly_Lib.A ();
			a.Foo ();
		}
	}
}