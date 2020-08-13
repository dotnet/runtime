using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[SetupLinkAttributesFile ("AddSuppressionsBeforeAttributeRemoval.xml")]
	[LogDoesNotContain ("IL2067: Mono.Linker.Tests.Cases.Warnings.WarningSuppression.AddSuppressionsBeforeAttributeRemoval.Main()")]
	public class AddSuppressionsBeforeAttributeRemoval
	{
		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (AddedPseudoAttributeAttribute);
		}

		[UnconditionalSuppressMessage ("ILLinker", "IL2067")]
		public static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}
	}
}
