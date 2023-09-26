using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: DebuggerTypeProxy (typeof (DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo.FooDebugView), Target = typeof (DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
	public class DebuggerTypeProxyAttributeOnAssemblyUsingTarget
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

			internal class FooDebugView
			{
				private Foo _foo;

				public FooDebugView (Foo foo)
				{
					_foo = foo;
				}
			}
		}
	}
}