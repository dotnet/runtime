// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace TLens.Analyzers
{
	sealed class InterfaceDispatchAnalyzer : InterfacesAnalyzer
	{
		public override void PrintResults (int maxCount)
		{
			var entries = usage.OrderBy (l => l.Value.Count).ThenByDescending (l => GetImplementationCount (l.Key)).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Possibly optimizable interface dispatch");

			foreach (var item in entries) {
				Console.WriteLine ($"Interface {item.Key.FullName} is implemented {GetImplementationCount (item.Key)} times and called only at");

				foreach (var location in item.Value) {
					Console.WriteLine ($"\t{location.ToDisplay ()}");
				}

				Console.WriteLine ();
			}
		}
	}
}
