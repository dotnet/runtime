using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
[assembly: DebuggerDisplay ("{Property}", TargetTypeName = "Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies.DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib+GenericType`1, library")]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers {
	[SetupLinkerCoreAction ("link")]
	[SetupLinkerKeepDebugMembers ("true")]
	[SetupCompileBefore ("library.dll", new [] { "../Dependencies/DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib.cs" })]

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	
	[KeptMemberInAssembly ("mscorlib.dll", typeof (DebuggerDisplayAttribute), ".ctor(System.String)")]
	[KeptMemberInAssembly ("mscorlib.dll", typeof (DebuggerDisplayAttribute), "set_TargetTypeName(System.String)")]
	
	[KeptMemberInAssembly ("library.dll", typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib.GenericType<>), "get_PropertyOnGenericType()")]
	[KeptMemberInAssembly ("library.dll", typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib.GenericType<>), "set_PropertyOnGenericType(T)")]
	public class DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameOfGenericTypeInOtherAssembly {
		public static void Main ()
		{
			var foo = new DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib.GenericType<int> ();
			foo.PropertyOnGenericType = 1;
		}
	}
}