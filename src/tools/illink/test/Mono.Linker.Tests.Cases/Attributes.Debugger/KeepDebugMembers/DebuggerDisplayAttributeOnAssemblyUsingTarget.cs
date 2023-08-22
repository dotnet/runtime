﻿using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
[assembly: DebuggerDisplay ("{Property}", Target = typeof (DebuggerDisplayAttributeOnAssemblyUsingTarget.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	[SetupLinkerTrimMode ("link")]
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), "set_Target(System.Type)")]
	public class DebuggerDisplayAttributeOnAssemblyUsingTarget
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
			public int Property { [Kept] get; [Kept] set; }
		}
	}
}