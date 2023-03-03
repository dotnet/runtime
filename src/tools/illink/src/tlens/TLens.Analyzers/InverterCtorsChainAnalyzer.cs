// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class InverterCtorsChainAnalyzer : Analyzer
	{
		readonly List<(MethodDefinition, MethodDefinition)> ctors = new List<(MethodDefinition, MethodDefinition)> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			if (!method.IsConstructor || !method.DeclaringType.IsClass)
				return;

			foreach (var instr in method.Body.Instructions) {
				switch (instr.OpCode.Code) {
				case Code.Call:
					var mr = (MethodReference) instr.Operand;
					var md = mr.Resolve ();
					if (md == null)
						return;

					if (!md.IsConstructor)
						return;

					if (md.DeclaringType != method.DeclaringType)
						return;

					if (md.Parameters.Count <= method.Parameters.Count)
						return;

					var prev = instr.Previous.OpCode.Code;
					if (prev == Code.Ldnull || prev == Code.Ldc_I4_0)
						ctors.Add ((method, md));

					break;
				}
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = ctors.OrderByDescending (l => l.Item2.GetEstimatedSize ()).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Constructors with possibly inverted initializations");

			foreach (var entry in entries) {
				Console.WriteLine ($"Constructor '{entry.Item1.ToDisplay ()}' calls possibly unnecessary initialization in '{entry.Item2.ToDisplay (showSize: true)}'");
			}
		}
	}
}
