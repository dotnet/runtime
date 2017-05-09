using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class)]
	public class NotATestCaseAttribute : BaseMetadataAttribute {
	}
}