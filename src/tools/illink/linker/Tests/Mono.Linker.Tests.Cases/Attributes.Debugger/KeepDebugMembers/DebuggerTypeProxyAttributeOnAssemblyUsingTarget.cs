using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (DebuggerTypeProxyAttribute))]
[assembly: DebuggerTypeProxy (typeof(DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo.FooDebugView), Target = typeof (DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers {
	[SetupLinkerCoreAction ("link")]
	[SetupLinkerKeepDebugMembers ("true")]
	
	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	
	[KeptMemberInAssembly ("mscorlib.dll", typeof (DebuggerTypeProxyAttribute), ".ctor(System.Type)")]
	[KeptMemberInAssembly ("mscorlib.dll", typeof (DebuggerTypeProxyAttribute), "set_Target(System.Type)")]
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
				[Kept]
				private Foo _foo;

				[Kept]
				public FooDebugView(Foo foo)
				{
					_foo = foo;
				}
			}
		}
	}
}