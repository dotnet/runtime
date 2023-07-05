// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class LimitedMethodCalls : Analyzer
	{
		readonly Dictionary<MethodDefinition, List<MethodDefinition>> methods = new Dictionary<MethodDefinition, List<MethodDefinition>> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			var instrs = method.Body.Instructions;

			foreach (var instr in instrs) {
				switch (instr.OpCode.Code) {
				case Code.Callvirt:
				case Code.Call:
					if (instr.Operand is not MethodReference mr)
						throw new NotImplementedException ();

					var md = mr.Resolve ();

					if (md == null)
						continue;

					if (md == method)
						continue;

					if (!methods.TryGetValue (md, out var existing)) {
						existing = new List<MethodDefinition> ();
						methods.Add (md, existing);
					}

					existing.Add (method);
					break;
				}
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = methods.Where (l => l.Value.Count <= 3).OrderBy (l => l.Value.Count).ThenByDescending (l => l.Key.GetEstimatedSize ()).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Methods called sparsely");

			foreach (var entry in entries) {
				Console.WriteLine ($"Method {entry.Key.ToDisplay (showSize: true)} is used only at");
				foreach (var values in entry.Value) {
					Console.WriteLine ($"\t{values.ToDisplay ()}");
				}

				Console.WriteLine ();
			}
		}
	}
}
