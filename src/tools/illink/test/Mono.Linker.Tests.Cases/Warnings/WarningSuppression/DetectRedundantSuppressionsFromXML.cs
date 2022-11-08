// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
	[ExpectedWarning ("IL2121", "IL2026", ProducedBy = ProducedBy.Trimmer, FileName = "DetectRedundantSuppressionsFromXML.xml", SourceLine = 7)]
	[ExpectedWarning ("IL2121", "IL2109", ProducedBy = ProducedBy.Trimmer, FileName = "DetectRedundantSuppressionsFromXML.xml", SourceLine = 12)]
	[SetupLinkAttributesFile ("DetectRedundantSuppressionsFromXML.xml")]
	public class DetectRedundantSuppressionsFromXML
	{
		public static void Main ()
		{
			DetectRedundantSuppressions.Test ();
		}

		public class DetectRedundantSuppressions
		{
			public static void Test ()
			{
				DoNotTriggerWarning ();
			}

			class SuppressedOnType : DoNotTriggerWarningType { }

			static void DoNotTriggerWarning () { }

			class DoNotTriggerWarningType { }
		}
	}
}