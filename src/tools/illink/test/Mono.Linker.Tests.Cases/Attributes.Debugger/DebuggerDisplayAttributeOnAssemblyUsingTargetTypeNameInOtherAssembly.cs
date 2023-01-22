using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if !NETCOREAPP
[assembly: KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
#endif

[assembly: DebuggerDisplay ("{Property}", TargetTypeName = "Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies.DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib, library")]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupCompileBefore ("library.dll", new[] { typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib) })]

#if NETCOREAPP
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
#else
	[SetupLinkerTrimMode ("link")]
	[SetupLinkerKeepDebugMembers ("false")]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (DebuggerDisplayAttribute), "set_TargetTypeName(System.String)")]
#endif

	[RemovedMemberInAssembly ("library.dll", typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib), "get_Property()")]
	[KeptMemberInAssembly ("library.dll", typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib), "set_Property(System.Int32)")]
	public class DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly
	{
		public static void Main ()
		{
			var foo = new DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib ();
			foo.Property = 1;
		}
	}
}