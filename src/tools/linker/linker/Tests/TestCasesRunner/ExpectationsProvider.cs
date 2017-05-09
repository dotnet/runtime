using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public static class ExpectationsProvider {

		public static bool IsAssemblyAssertion (CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof (KeptAssemblyAttribute) || attr.AttributeType.Name == nameof (RemovedAssemblyAttribute);
		}
	}
}
