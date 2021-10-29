// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	class InverterCtorsChainAnalyzer : Analyzer
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