using System;


namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptMemberInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute {
		public readonly string AssemblyFileName;
		public readonly string TypeName;
		public readonly string [] MemberNames;

		public KeptMemberInAssemblyAttribute (string assemblyFileName, Type type, params string [] memberNames)
		{
			AssemblyFileName = assemblyFileName;
			TypeName = type.ToString ();
			MemberNames = memberNames;
		}

		public KeptMemberInAssemblyAttribute (string assemblyFileName, string typeName, params string [] memberNames)
		{
			AssemblyFileName = assemblyFileName;
			TypeName = typeName;
			MemberNames = memberNames;
		}
	}
}
