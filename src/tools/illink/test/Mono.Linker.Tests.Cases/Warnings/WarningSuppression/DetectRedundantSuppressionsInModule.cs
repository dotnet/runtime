// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[assembly: ExpectedWarning ("IL2121", "IL2071", Tool.Trimmer, "")]
[module: UnconditionalSuppressMessage ("Test", "IL2071:Redundant suppression, warning is not issued in this assembly")]


namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsInModule
	{
		public static void Main ()
		{
			TrimmerCompatibleMethod ();
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}
	}
}
