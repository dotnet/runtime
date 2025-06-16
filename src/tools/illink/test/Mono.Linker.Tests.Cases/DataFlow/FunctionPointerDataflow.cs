// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupCompileArgument ("/unsafe")]
	[Kept]
	[ExpectedNoWarnings]
	unsafe class FunctionPointerDataflow
	{
		public static void Main ()
		{
			UseFnPtr ();
		}

		[Kept]
		static unsafe delegate*<void> fnptr1;
		[Kept]
		static unsafe delegate*<void> fnptr2;

		[Kept]
		[ExpectedWarning ("IL2026", nameof (RUC))]
		static unsafe void UseFnPtr (bool b = true)
		{
			delegate*<void> f = fnptr1;
			if (b) {
				f = fnptr2;
			}
			f ();
			RUC ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("")]
		static void RUC () { }
	}
}
