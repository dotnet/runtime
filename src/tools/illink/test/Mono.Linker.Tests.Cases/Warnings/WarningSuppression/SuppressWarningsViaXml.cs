using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
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
