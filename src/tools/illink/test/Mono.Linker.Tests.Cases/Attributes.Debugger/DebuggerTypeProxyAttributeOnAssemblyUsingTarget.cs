using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute(typeof(DebuggerTypeProxyAttribute))]
[assembly: DebuggerTypeProxy (typeof(DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo.FooDebugView), Target = typeof (DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger {
	[SetupLinkerCoreAction ("link")]
	[SetupLinkerKeepDebugMembers ("false")]
	
	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	
	[KeptMemberInAssembly ("mscorlib.dll", typeof (DebuggerTypeProxyAttribute), ".ctor(System.Type)")]
	public class DebuggerTypeProxyAttributeOnAssemblyUsingTarget {
		public static void Main ()
		{
			var foo = new Foo ();
			foo.Property = 1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class Foo {
			[Kept]
			[KeptBackingField]
			public int Property { get; [Kept] set; }

			[Kept]
			internal class FooDebugView
			{
				private Foo _foo;

				public FooDebugView(Foo foo)
				{
					_foo = foo;
				}
			}
		}
	}
}