// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;


namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[SetupLinkerArgument ("--singlewarn")]
	[LogContains ("warning IL2104: Assembly 'test' produced trim warnings")]
	[LogDoesNotContain ("IL2121")]
	class DetectRedundantSuppressionsSingleWarn
	{
		public static void Main ()
		{
			TrimmerCompatibleMethod ();
		}

		[UnconditionalSuppressMessage ("test", "IL2072")]
		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}
	}
}
