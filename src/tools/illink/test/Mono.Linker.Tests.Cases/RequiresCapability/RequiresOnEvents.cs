// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[ExpectedNoWarnings]
	public class RequiresOnEvents
	{
		public static void Main ()
		{
			Test ();
		}

		[Kept]
		static event EventHandler EventToTestAdd {
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[KeptAttributeAttribute (typeof (RequiresAssemblyFilesAttribute))]
			[KeptAttributeAttribute (typeof (RequiresDynamicCodeAttribute))]
			[RequiresUnreferencedCode ("Message for --EventToTestAdd.add--")]
			[RequiresAssemblyFiles ("Message for --EventToTestAdd.add--")]
			[RequiresDynamicCode ("Message for --EventToTestAdd.add--")]
			[Kept (By = Tool.Trimmer)]
			add { }
			[Kept]
			remove { }
		}

		[Kept]
		public static void Test ()
		{
			// Marking remove marks the event and all its methods in the trimmer
			// This should warn for the add method even if it's not explicitly used
			// However, for NativeAOT, add is removed, so it shouldn't warn on NativeAOT
			EventToTestAdd -= (sender, e) => { };
		}
	}
}
