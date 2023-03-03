using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	[SetupLinkerDescriptorFile ("WorksWithLinkXml.xml")]
	public class WorksWithLinkXml
	{
		public static void Main ()
		{
		}

		[Kept]
		class Foo
		{
			[Kept]
			[ExpectBodyModified]
			public void InstanceMethod ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}