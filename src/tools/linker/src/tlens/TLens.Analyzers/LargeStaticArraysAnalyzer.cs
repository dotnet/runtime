// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class LargeStaticArraysAnalyzer : Analyzer
	{
		readonly List<(int, MethodDefinition)> methods = new List<(int, MethodDefinition)> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			foreach (var instr in method.Body.Instructions) {
				if (instr.OpCode.Code != Code.Ldtoken)
					continue;

				if (instr.Operand is not FieldReference fr)
					continue;

				var name = fr.FieldType.Name;
				if (!name.StartsWith ("__StaticArrayInitTypeSize="))
					continue;

				if (!int.TryParse (name.AsSpan (26), out int size))
					throw new NotImplementedException (name);

				methods.Add ((size, method));
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = methods.OrderByDescending (l => l.Item1).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Largest static arrays");

			foreach (var entry in entries) {
				Console.WriteLine ($"{entry.Item1} bytes large array is initialized in {entry.Item2}");
			}
		}
	}
}
