using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Metadata
{
	[VerifyMetadataNames]
	[SetupLinkerDescriptorFile ("RootDescriptorNamesAreKept.xml")]
	public class RootDescriptorNamesAreKept
	{
		public static void Main ()
		{
		}

		[KeptMember (".ctor()")]
		class RootedType
		{
			[Kept]
			void InstanceMethodWithKeptParameterName (int arg)
			{
			}

			[Kept]
			static void MethodWithKeptParameterName (string str)
			{
			}
		}

		[KeptMember (".ctor()")]
		class TypeWithPreserveMethods
		{
			[Kept]
			void InstanceMethodWithKeptParameterName (int arg)
			{
			}

			[Kept]
			static void MethodWithKeptParameterName (string str)
			{
			}
		}
	}
}
