using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerArgument ("--exclude-feature", "test-1-feature-name")]
	public class FeatureExcludedSimpleType
	{
		public static void Main ()
		{
		}
	}

	public class FeatureExcludedSimpleTypeTestClass
	{
		public static void Foo ()
		{
		}
	}
}
