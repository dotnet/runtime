using System;
using System.Diagnostics;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[Conditional("INCLUDE_EXPECTATIONS")]
	public abstract class BaseMetadataAttribute : Attribute {
	}
}
