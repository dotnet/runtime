using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: DebuggerDisplay ("{Property}", Target = typeof (DebuggerDisplayAttributeOnAssemblyUsingTarget.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
	public class DebuggerDisplayAttributeOnAssemblyUsingTarget
	{
		public static void Main ()
		{
			var foo = new Foo ();
			foo.Property = 1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class Foo
		{
			[Kept]
			[KeptBackingField]
			public int Property { get; [Kept] set; }
		}
	}
}