// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Repro
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class Program
	{

		public static void Main ()
		{
			Console.WriteLine ("HelloWorld");
		}
	}
}
