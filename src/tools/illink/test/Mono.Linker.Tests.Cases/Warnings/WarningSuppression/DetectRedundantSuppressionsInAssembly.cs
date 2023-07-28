﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[assembly: ExpectedWarning ("IL2121", "IL2071", ProducedBy = Tool.Trimmer)]
[assembly: UnconditionalSuppressMessage ("Test", "IL2071:Redundant suppression, warning is not issued in this assembly")]


namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class DetectRedundantSuppressionsInAssembly
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
