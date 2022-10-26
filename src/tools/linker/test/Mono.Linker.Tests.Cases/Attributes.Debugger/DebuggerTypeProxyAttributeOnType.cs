using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
#if NETCOREAPP
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
#else
	[SetupLinkerTrimMode ("link")]
	[SetupLinkerKeepDebugMembers ("false")]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerTypeProxyAttribute), ".ctor(System.Type)")]
#endif
	public class DebuggerTypeProxyAttributeOnType
	{
		public static void Main ()
		{
			var foo = new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
#if !NETCOREAPP
		[KeptAttributeAttribute (typeof (DebuggerTypeProxyAttribute))]
#endif
		[DebuggerTypeProxy (typeof (FooDebugView))]
		class Foo
		{
		}

#if !NETCOREAPP
		[Kept]
#endif
		class FooDebugView
		{
			public FooDebugView (Foo foo)
			{
			}
		}
	}
}