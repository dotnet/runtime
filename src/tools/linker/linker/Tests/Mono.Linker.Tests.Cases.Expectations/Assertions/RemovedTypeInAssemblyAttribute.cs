using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class RemovedTypeInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute {
		public readonly string AssemblyFileName;
		public readonly string TypeName;

		public RemovedTypeInAssemblyAttribute (string assemblyFileName, Type type)
		{
			AssemblyFileName = assemblyFileName;
			TypeName = type.ToString ();
		}

		public RemovedTypeInAssemblyAttribute (string assemblyFileName, string typeName)
		{
			AssemblyFileName = assemblyFileName;
			TypeName = typeName;
		}
	}
}
