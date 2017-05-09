using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class RemovedMemberInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute {
		public readonly string AssemblyFileName;
		public readonly Type Type;
		public readonly string [] MemberNames;

		public RemovedMemberInAssemblyAttribute (string assemblyFileName, Type type, params string [] memberNames)
		{
			AssemblyFileName = assemblyFileName;
			Type = type;
			MemberNames = memberNames;
		}
	}
}
