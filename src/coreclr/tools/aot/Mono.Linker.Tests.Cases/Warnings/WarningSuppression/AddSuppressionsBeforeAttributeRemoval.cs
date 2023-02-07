using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SetupLinkAttributesFile ("AddSuppressionsBeforeAttributeRemoval.xml")]

	[ExpectedNoWarnings]
	public class AddSuppressionsBeforeAttributeRemoval
	{
		[Kept]
		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (AddedPseudoAttributeAttribute);
		}

		[UnconditionalSuppressMessage ("ILLinker", "IL2072")]
		public static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}
	}
}
