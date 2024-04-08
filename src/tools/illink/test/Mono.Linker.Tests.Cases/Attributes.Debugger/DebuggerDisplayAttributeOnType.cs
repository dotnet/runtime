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
			var fooBaz = new FooBaz ();
			var fooBar = new FooBar ();
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

		[Kept]
		[KeptMember (".ctor()")]
		[DebuggerDisplay ("_", Name="{Property}")]
		class FooBaz
		{
			public int Property { get; set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[DebuggerDisplay ("_", Type="{Property}")]
		class FooBar
		{
			public int Property { get; set; }
		}
	}
}
