using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
	[SetupLinkerAction ("copy", "test")]
	public static class DebuggerDisplayAttributeOnTypeCopy
	{
		public static void Main ()
		{
			var foo = new Foo ();
			var bar = new Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("{Property}")]
		class Foo
		{
			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("{Method()}")]
		class Bar
		{
			[Kept]
			public int Method ()
			{
				return 1;
			}
		}
	}
}
