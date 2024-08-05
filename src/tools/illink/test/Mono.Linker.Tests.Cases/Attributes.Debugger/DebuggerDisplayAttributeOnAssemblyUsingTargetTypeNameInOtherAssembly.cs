using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: DebuggerDisplay ("{Property}", TargetTypeName = "Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies.DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib, library")]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupCompileBefore ("library.dll", new[] { typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib) })]
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]

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