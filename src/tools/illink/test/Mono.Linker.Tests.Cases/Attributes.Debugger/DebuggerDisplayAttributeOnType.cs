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

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
#endif
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
#if !NETCOREAPP
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
#endif
		[DebuggerDisplay ("{Property}")]
		class Foo
		{
			public int Property { get; set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
#if !NETCOREAPP
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
#endif
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
#if !NETCOREAPP
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
#endif
		[DebuggerDisplay (null)]
		class Baz
		{
		}
	}
}