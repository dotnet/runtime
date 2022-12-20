using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	/// <summary>
	/// [ThreadStatic] is required by the mono runtime
	/// </summary>
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	public class ThreadStaticIsPreservedOnField
	{
		[ThreadStatic]
		[Kept]
		[KeptAttributeAttribute (typeof (ThreadStaticAttribute))]
		public static int UsedField;

		public static void Main ()
		{
			UsedField = 0;
		}
	}
}