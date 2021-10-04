// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace TLens.Analyzers
{
	class InterfaceTypeCheckAnalyzers : InterfacesAnalyzer
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