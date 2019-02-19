using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class SetupLinkerLinkPublicAndFamilyAttribute : BaseMetadataAttribute {
	}
}