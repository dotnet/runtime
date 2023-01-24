// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Similar to CompilerGeneratedCodeInPreservedAssembly, but with warnings
	// produced while marking the compiler generated code.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SetupLinkerDescriptorFile ("CompilerGeneratedCodeInPreservedAssembly.xml")]
	class CompilerGeneratedCodeInPreservedAssemblyWithWarning
	{
		[ExpectedWarning ("IL2026", "--" + nameof (Inner) + "." + nameof (Inner.WithLocalFunctionInner) + "--")]
		[ExpectedWarning ("IL2026", "--" + nameof (WithLocalFunction) + "--")]
		public static void Main ()
		{
			Inner.WithLocalFunctionInner ();
			WithLocalFunction ();
		}

		// The compiler generated state will see the modified body,
		// and will not associate the local function with the user method.
		// Generic argument warnings from the local function will not be suppressed
		// by RUC on the user method.

		class Inner
		{
			[RequiresUnreferencedCode ("--" + nameof (Inner) + "." + nameof (WithLocalFunctionInner) + "--")]
			public static void WithLocalFunctionInner ()
			{
				if (AlwaysFalse) {
					LocalWithWarning<int> ();
				}

				// https://github.com/dotnet/linker/issues/2937
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer)]
				void LocalWithWarning<T> ()
				{
					// Warning!
					RequiresAllOnT<T> ();
				}
			}
		}

		[RequiresUnreferencedCode ("--" + nameof (WithLocalFunction) + "--")]
		public static void WithLocalFunction ()
		{
			if (AlwaysFalse) {
				LocalWithWarning<int> ();
			}

			// https://github.com/dotnet/linker/issues/2937
			[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer)]
			void LocalWithWarning<T> ()
			{
				// No warning
				RequiresAllOnT<T> ();
			}
		}
		public static bool AlwaysFalse => false;
		static void RequiresAllOnT<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T> () { }
	}
}
