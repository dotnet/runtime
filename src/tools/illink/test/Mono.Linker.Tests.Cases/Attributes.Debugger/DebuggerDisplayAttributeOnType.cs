using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
	public class DebuggerDisplayAttributeOnType
	{
		public static void Main ()
		{
			var foo = new Foo ();
			var bar = new Bar ();
			var baz = new Baz ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[DebuggerDisplay ("{Property}")]
		class Foo
		{
			public int Property { get; set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[DebuggerDisplay ("{Method()}")]
		class Bar
		{
			public int Method ()
			{
				return 1;
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[DebuggerDisplay (null)]
		class Baz
		{
		}
	}
}