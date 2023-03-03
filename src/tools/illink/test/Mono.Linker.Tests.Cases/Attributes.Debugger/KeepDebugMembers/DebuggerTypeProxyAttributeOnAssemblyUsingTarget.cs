using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (DebuggerTypeProxyAttribute))]
[assembly: DebuggerTypeProxy (typeof (DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo.FooDebugView), Target = typeof (DebuggerTypeProxyAttributeOnAssemblyUsingTarget.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	[SetupLinkerTrimMode ("link")]
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerTypeProxyAttribute), ".ctor(System.Type)")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerTypeProxyAttribute), "set_Target(System.Type)")]
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

			[Kept]
			internal class FooDebugView
			{
				[Kept]
				private Foo _foo;

				[Kept]
				public FooDebugView (Foo foo)
				{
					_foo = foo;
				}
			}
		}
	}
}