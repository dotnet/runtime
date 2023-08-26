using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
	public class DebuggerTypeProxyAttributeOnType
	{
		public static void Main ()
		{
			var foo = new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[DebuggerTypeProxy (typeof (FooDebugView))]
		class Foo
		{
		}

		class FooDebugView
		{
			public FooDebugView (Foo foo)
			{
			}
		}
	}
}