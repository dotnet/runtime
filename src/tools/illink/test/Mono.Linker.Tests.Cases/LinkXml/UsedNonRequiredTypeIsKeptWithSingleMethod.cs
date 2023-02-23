using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UsedNonRequiredTypeIsKeptWithSingleMethod.xml")]
	[SetupLinkerArgument ("--disable-opt", "unreachablebodies")]
	class UsedNonRequiredTypeIsKeptWithSingleMethod
	{
		public static void Main ()
		{
			var t = typeof (Unused);
		}

		[Kept]
		class Unused
		{
			[Kept]
			private void PreservedMethod ()
			{
				new SecondLevel (2);
			}
		}

		[Kept]
		class SecondLevel
		{
			[Kept]
			public SecondLevel (int arg)
			{
			}
		}

		// NativeAOT should generate conditional dependencies for the tag required
		// https://github.com/dotnet/runtime/issues/80464
		[Kept (By = ProducedBy.NativeAot)]
		class ReallyUnused
		{
			[Kept (By = ProducedBy.NativeAot)]
			private void PreservedMethod ()
			{
				new SecondLevelUnused (2);
			}
		}

		// NativeAOT should generate conditional dependencies for the tag required
		// https://github.com/dotnet/runtime/issues/80464
		[Kept (By = ProducedBy.NativeAot)]
		class SecondLevelUnused
		{
			[Kept (By = ProducedBy.NativeAot)]
			public SecondLevelUnused (int arg)
			{
			}
		}
	}
}
