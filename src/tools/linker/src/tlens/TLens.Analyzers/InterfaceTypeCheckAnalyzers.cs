// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace TLens.Analyzers
{
	sealed class InterfaceTypeCheckAnalyzers : InterfacesAnalyzer
	{
		public override void PrintResults (int maxCount)
		{
			var entries = interfaces.Keys.Where (l => !usage.ContainsKey (l)).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Never Referenced Interface Types");
			foreach (var iface in entries) {
				Console.WriteLine ($"Unused interface type '{iface.FullName}' is implemented by");
				foreach (var type in interfaces[iface]) {
					Console.WriteLine ($"\t{type.FullName}");
				}

				Console.WriteLine ();
			}
		}
	}
}
