using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct)]
	public class NotATestCaseAttribute : BaseMetadataAttribute {
	}
}