// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class DuplicatedCodeAnalyzer : Analyzer
	{
		readonly Dictionary<string, List<MethodDefinition>> strings = new Dictionary<string, List<MethodDefinition>> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			// TODO: Add more cases to detect potencial code duplications
			foreach (var instr in method.Body.Instructions) {
				switch (instr.OpCode.Code) {
				case Code.Ldstr:
					var str = (string) instr.Operand;

					// Short strings are hard to identify as true duplicates
					if (str.Length < 3)
						continue;

					// It'd common to throw exception referencing parameter name
					if (method.HasParameters && method.Parameters.Any (l => l.Name == str))
						continue;

					if (!strings.TryGetValue (str, out List<MethodDefinition> existing)) {
						existing = new List<MethodDefinition> ();
						strings.Add (str, existing);
					}

					if (!existing.Contains (method))
						existing.Add (method);

					break;
				}
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = strings.Where (l => l.Value.Count > 1).OrderByDescending (l => l.Value.Count).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Possibly duplicated logic in strings handling");
			foreach (var entry in entries) {
				Console.WriteLine ($"String value \"{entry.Key}\"");
				foreach (var m in entry.Value)
					Console.WriteLine ($"\tMethod '{m.ToDisplay ()}'");
				Console.WriteLine ();
			}
		}
	}
}
