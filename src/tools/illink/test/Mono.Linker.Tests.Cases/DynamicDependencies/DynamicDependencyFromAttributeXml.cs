using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	// For netcoreapp we don't have to specify the assembly for the attribute, since the attribute comes from corelib
	// and will be found always.
	// For mono though, we have to specify the assembly (Mono.Linker.Tests.Cases.Expectations) because at the time of processing
	// that assembly is not yet loaded into the closure in ILLink, so it won't find the attribute type.
#if NET
	[SetupLinkAttributesFile ("DynamicDependencyFromAttributeXml.netcore.Attributes.xml")]
#else
	[SetupLinkAttributesFile ("DynamicDependencyFromAttributeXml.mono.Attributes.xml")]
#endif
	[IgnoreLinkAttributes (false)]
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies", "missing.dll")]
	class DynamicDependencyFromAttributeXml
	{
		public static void Main ()
		{
			DependencyToUnusedMethod ();
			DependencyToUnusedType ();
		}

		[Kept]
		static void DependencyToUnusedMethod ()
		{
		}

		// https://github.com/dotnet/runtime/issues/79393
		[Kept (By = Tool.Trimmer)]
		static void UnusedMethod ()
		{
		}

		[Kept]
		static void DependencyToUnusedType ()
		{
		}

		class NonUsedType
		{
			// https://github.com/dotnet/runtime/issues/79393
			[Kept (By = Tool.Trimmer)]
			public NonUsedType ()
			{
			}

			// https://github.com/dotnet/runtime/issues/79393
			[Kept (By = Tool.Trimmer)]
			public static void PleasePreserveThisMethod ()
			{
			}
		}
	}
}
