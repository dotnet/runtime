// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class LargeStringsAnalyzer : Analyzer
	{
		readonly List<(int, MethodDefinition)> ldstrs = new List<(int, MethodDefinition)> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			var reported = new List<string> ();
			foreach (var instr in method.Body.Instructions) {
				switch (instr.OpCode.Code) {
				case Code.Ldstr:
					string str = (string) instr.Operand;
					if (reported.Contains (str))
						break;

					if (str.Length > 1)
						ldstrs.Add ((str.Length, method));

					reported.Add (str);
					break;
				}
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = ldstrs.OrderByDescending (l => l.Item1).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Methods with large string loads");

			foreach (var entry in entries) {
				Console.WriteLine ($"Method '{entry.Item2.ToDisplay ()}' loads '{entry.Item1}' characters long string");
			}
		}
	}
}
