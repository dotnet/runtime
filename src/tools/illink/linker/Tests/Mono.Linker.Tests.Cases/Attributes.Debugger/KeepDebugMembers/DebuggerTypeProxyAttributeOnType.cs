using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers {
	[SetupLinkerCoreAction ("link")]
	[SetupLinkerKeepDebugMembers ("true")]
	
	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	
	[KeptMemberInAssembly ("mscorlib.dll", typeof (DebuggerTypeProxyAttribute), ".ctor(System.Type)")]
	public class DebuggerTypeProxyAttributeOnType {
		public static void Main ()
		{
			var foo = new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerTypeProxyAttribute))]
		[DebuggerTypeProxy (typeof (FooDebugView))]
		class Foo {
		}
		
		[Kept]
		class FooDebugView {
			[Kept]
			public FooDebugView (Foo foo)
			{
			}
		}
	}
}