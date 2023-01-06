
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

[assembly: VerifyAttributesInAssemblyWorks_Base.ForAssertingKept]
[assembly: VerifyAttributesInAssemblyWorks_Base.ForAssertingRemove]

namespace Mono.Linker.Tests.Cases.TestFramework.Dependencies
{
	public class VerifyAttributesInAssemblyWorks_Lib
	{
		[VerifyAttributesInAssemblyWorks_Base.ForAssertingKept]
		public static class TypeWithKeptAttribute
		{
			[VerifyAttributesInAssemblyWorks_Base.ForAssertingKept]
			public static int Field;

			[VerifyAttributesInAssemblyWorks_Base.ForAssertingKept]
			public static void Method ()
			{
			}

			[VerifyAttributesInAssemblyWorks_Base.ForAssertingKept]
			public static int Property { get; set; }
		}

		[VerifyAttributesInAssemblyWorks_Base.ForAssertingRemove]
		public class TypeWithRemovedAttribute
		{
			[VerifyAttributesInAssemblyWorks_Base.ForAssertingRemove]
			public static int Field;

			[VerifyAttributesInAssemblyWorks_Base.ForAssertingRemove]
			public static void Method ()
			{
			}

			[VerifyAttributesInAssemblyWorks_Base.ForAssertingRemove]
			public static int Property { get; set; }
		}
	}
}