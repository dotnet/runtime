using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	[SetupLinkerTrimMode ("link")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
	public class DebuggerDisplayAttributeOnType
	{
		public static void Main ()
		{
			var foo = new Foo ();
			var bar = new Bar ();
			var fooBaz = new FooBaz ();
			var fooBar = new FooBar ();
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

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("_", Name="{Property}")]
		class FooBaz
		{
			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("_", Type="{Property}")]
		class FooBar
		{
			[Kept]
			[KeptBackingField]
			public int Property { [Kept] get; [Kept] set; }
		}
	}
}
