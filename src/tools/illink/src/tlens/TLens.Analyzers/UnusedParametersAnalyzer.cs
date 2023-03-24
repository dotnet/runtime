// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class UnusedParametersAnalyzer : Analyzer
	{
		readonly List<(MethodDefinition, int)> methods = new List<(MethodDefinition, int)> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			if (!method.HasParameters)
				return;

			if (method.IsVirtual || method.IsInternalCall)
				return;

			if (method.HasCustomAttributes && method.CustomAttributes.Any (l => l.AttributeType.Name == "IntrinsicAttribute"))
				return;

			var parameters = new BitArray (method.Parameters.Count);
			foreach (var instr in method.Body.Instructions) {
				int index;
				switch (instr.OpCode.Code) {
				default:
					continue;
				case Code.Ldarg_0:
					if (!method.IsStatic)
						continue;

					index = 0;
					break;
				case Code.Ldarg_1:
					index = method.IsStatic ? 1 : 0;
					break;
				case Code.Ldarg_2:
					index = method.IsStatic ? 2 : 1;
					break;
				case Code.Ldarg_3:
					index = method.IsStatic ? 3 : 2;
					break;
				case Code.Ldarg_S:
				case Code.Ldarga_S:
					index = ((ParameterDefinition) instr.Operand).Index;
					break;
				}

				parameters[index] = true;
			}

			for (int i = 0; i < parameters.Count; ++i) {
				if (parameters[i])
					continue;

				methods.Add ((method, i));
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = methods.OrderBy (l => l.Item1.Parameters[l.Item2].ParameterType.IsPrimitive).ThenBy (l => l.Item1.FullName).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Method unused parameters");

			foreach (var entry in entries) {
				var method = entry.Item1;
				Console.WriteLine ($"Method '{method.ToDisplay ()}' has unused parameter '{method.Parameters[entry.Item2].Name}'");
			}
		}
	}
}
