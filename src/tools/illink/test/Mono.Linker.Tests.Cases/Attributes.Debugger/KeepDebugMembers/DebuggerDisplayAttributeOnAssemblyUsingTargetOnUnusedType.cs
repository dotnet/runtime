using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: DebuggerDisplay ("{Property}", Target = typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetOnUnusedType.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	[SetupLinkerTrimMode ("link")]
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

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