using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class RemovedTypeInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute {
		public readonly string AssemblyFileName;
		public readonly Type Type;

		public RemovedTypeInAssemblyAttribute (string assemblyFileName, Type type)
		{
			AssemblyFileName = assemblyFileName;
			Type = type;
		}
	}
}
