using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	[SetupLinkerTrimMode ("link")]
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
	public class DebuggerDisplayAttributeOnType
	{
		public static void Main ()
		{
			var foo = new Foo ();
			var bar = new Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("{Property}")]
		class Foo
		{
			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("{Method()}")]
		class Bar
		{
			[Kept]
			public int Method ()
			{
				return 1;
			}
		}
	}
}