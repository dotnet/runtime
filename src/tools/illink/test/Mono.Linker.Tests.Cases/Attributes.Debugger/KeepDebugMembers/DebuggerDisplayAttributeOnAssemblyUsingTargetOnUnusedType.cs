using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: DebuggerDisplay ("{Property}", Target = typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetOnUnusedType.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	[SetupLinkerTrimMode ("link")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
	public class DebuggerDisplayAttributeOnAssemblyUsingTargetOnUnusedType
	{
		public static void Main ()
		{
		}

		public class Foo
		{
			public int Property { get; set; }
		}
	}
}