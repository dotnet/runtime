// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	[Define ("DEBUG")]
	public class InterpolatedStringHandlerDataFlow
	{
		public static void Main ()
		{
			Test ();
		}

		static void Test(bool b = true) {
			// Creates a control-flow graph for the analyzer that has an
			// IFlowCaptureReferenceOperation that represents a capture
			// because it is used as an out param (so has IsInitialization = true).
			// See https://github.com/dotnet/roslyn/issues/57484 for context.
			// This test ensures the analyzer has coverage for cases
			// where IsInitialization = true.
			Debug.Assert (b, $"Debug interpolated string handler {b}");
		}
	}
}
