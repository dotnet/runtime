using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field,
		AllowMultiple = true,
		Inherited = false)]
	public class ExpectedWarningAttribute : EnableLoggerAttribute
	{
		public ExpectedWarningAttribute (string warningCode, params string[] messageContains)
		{
		}

		public string FileName { get; set; }
		public int SourceLine { get; set; }
		public int SourceColumn { get; set; }
	}
}
