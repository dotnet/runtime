using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedNonRequiredTypeIsRemoved.xml")]
	public class UnusedNonRequiredTypeIsRemoved
	{
		public static void Main ()
		{
		}

		// NativeAOT should generate conditional dependencies for the required tag
		// https://github.com/dotnet/runtime/issues/80464
		[Kept (By = Tool.NativeAot)]
		[KeptMember (".ctor()", By = Tool.NativeAot)]
		class Unused
		{
		}
	}
}
