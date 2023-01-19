using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[SetupLinkAttributesFile ("SuppressWarningsViaXml.xml")]
	public class SuppressWarningsViaXml
	{
		public static void Main ()
		{
			SuppressedOnMethod ();
			var t = typeof (SuppressedOnType);
		}

		static void SuppressedOnMethod ()
		{
			TriggerWarning ();
		}

		class SuppressedOnType : TriggerWarningType { }

		[RequiresUnreferencedCode ("--TriggerWarning--")]
		static void TriggerWarning () { }

		[RequiresUnreferencedCode ("--TriggerWarningType--")]
		class TriggerWarningType { }
	}
}