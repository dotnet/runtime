// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class UserOperatorCalledForNullCheckAnalyzer : Analyzer
	{
		sealed class Counters
		{
			public int Total;
			public int Redundant;

			public double Ratio => (double) Redundant / Total;
		}

		readonly Dictionary<MethodDefinition, Counters> operators = new Dictionary<MethodDefinition, Counters> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			var instrs = method.Body.Instructions;

			foreach (var instr in instrs) {
				switch (instr.OpCode.Code) {
				case Code.Call:
					if (instr.Operand is not MethodReference mr)
						throw new NotImplementedException ();

					if (mr.Name != "op_Equality" && mr.Name != "op_Inequality")
						continue;

					var md = mr.Resolve ();
					if (md.Parameters.Count != 2)
						continue;

					if (!operators.TryGetValue (md, out Counters data)) {
						data = new Counters ();
						operators.Add (md, data);
					}

					if (instr.Previous.OpCode.Code == Code.Ldnull) {
						data.Redundant++;
					}

					data.Total++;
					break;
				}
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = operators.Where (l => l.Value.Ratio > 0).OrderByDescending (l => l.Value.Ratio).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("User operators used for null checks");

			foreach (var e in entries) {
				Console.WriteLine ($"User operator '{e.Key.ToDisplay ()}' was called {e.Value.Redundant} [{e.Value.Ratio.ToString ("0%")}] times with null values");
			}
		}
	}
}
