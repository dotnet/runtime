// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using TLens.Analyzers;

namespace TLens
{
	class Runner
	{
		readonly List<Analyzer> analyzers = new List<Analyzer> ();

		public void AddAnalyzer (Analyzer analyzer)
		{
			analyzers.Add (analyzer);
		}

		public void AddAnalyzers (IEnumerable<Analyzer> analyzers)
		{
			this.analyzers.AddRange (analyzers);
		}

		public int MaxAnalyzerResults { get; set; } = 30;

		public void Process (List<AssemblyDefinition> assemblies)
		{
			if (assemblies.Count == 0)
				return;

			bool first = true;
			foreach (var a in analyzers) {
				foreach (var assembly in assemblies) {
					try {
						a.ProcessAssembly (assembly);
					} catch (Exception e) {
						throw new ApplicationException ($"Internal error when analyzing '{assembly.FullName}' assembly with '{a.GetType ()}'", e);
					}
				}

				if (!first)
					Console.WriteLine ();

				a.PrintResults (MaxAnalyzerResults);
				first = false;
			}
		}
	}
}
