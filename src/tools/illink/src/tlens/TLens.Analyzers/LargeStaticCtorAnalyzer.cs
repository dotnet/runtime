// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace TLens.Analyzers
{
	sealed class LargeStaticCtorAnalyzer : Analyzer
	{
		readonly List<MethodDefinition> cctors = new List<MethodDefinition> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			if (method.Name != ".cctor")
				return;

			cctors.Add (method);
		}

		public override void PrintResults (int maxCount)
		{
			var entries = cctors.OrderByDescending (l => l.GetEstimatedSize ()).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Large static constructors");

			foreach (var m in entries) {
				Console.WriteLine (m.ToDisplay (showSize: true));
			}
		}
	}
}
