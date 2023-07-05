using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UsedNonRequiredTypeIsKept.xml")]
	public class UsedNonRequiredTypeIsKept
	{
		public static void Main ()
		{
			var tmp = typeof (Used1).ToString ();
			tmp = typeof (Used2).ToString ();
			tmp = typeof (Used3).ToString ();
		}

		class Used1
		{
			[Kept]
			public int field;

			public void Method ()
			{
			}

			public int Property { get; set; }
		}

		[KeptMember (".ctor()")]
		class Used2
		{
			public int field;

			[Kept]
			public void Method ()
			{
			}

			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }
		}

		[KeptMember (".ctor()")]
		class Used3
		{
			[Kept]
			public int field;

			[Kept]
			public void Method ()
			{
			}

			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }
		}
	}
}