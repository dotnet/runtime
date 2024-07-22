using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Metadata
{
	[VerifyMetadataNames]
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[KeptMember (".ctor()")]
	public class RootLibraryAssemblyNamesAreKept
	{
		public static void Main ()
		{
		}

		[Kept]
		public void InstanceMethodWithKeptParameterName (int arg)
		{
		}

		[Kept]
		public static void MethodWithKeptParameterName (string str)
		{
		}
	}
}
