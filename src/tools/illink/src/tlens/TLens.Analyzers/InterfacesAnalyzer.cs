// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	abstract class InterfacesAnalyzer : Analyzer
	{
		protected readonly Dictionary<TypeDefinition, List<TypeDefinition>> interfaces = new Dictionary<TypeDefinition, List<TypeDefinition>> ();
		protected readonly Dictionary<TypeDefinition, HashSet<MethodDefinition>> usage = new Dictionary<TypeDefinition, HashSet<MethodDefinition>> ();

		protected override void ProcessType (TypeDefinition type)
		{
			if (!type.HasInterfaces)
				return;

			foreach (var iface in type.Interfaces) {
				TypeDefinition id = iface.InterfaceType.Resolve ();
				if (id == null)
					continue;

				if (!interfaces.TryGetValue (id, out var types)) {
					types = new List<TypeDefinition> ();
					interfaces.Add (id, types);
				}

				types.Add (type);
			}
		}

		protected override void ProcessMethod (MethodDefinition method)
		{
			var instrs = method.Body.Instructions;
			TypeDefinition td;

			foreach (var instr in instrs) {
				switch (instr.OpCode.OperandType) {
				case OperandType.InlineType:
				case OperandType.InlineTok:
					var tr = instr.Operand as TypeReference;
					td = tr?.Resolve ();
					break;

				case OperandType.InlineMethod:
					if (instr.Operand is not MethodReference mr)
						continue;

					td = mr.Resolve ()?.DeclaringType;
					break;
				default:
					continue;
				}

				if (td == null || !td.IsInterface)
					continue;

				if (!usage.ContainsKey (td))
					usage.Add (td, new HashSet<MethodDefinition> ());

				usage[td].Add (method);
			}
		}

		protected int GetImplementationCount (TypeDefinition iface)
		{
			return interfaces.TryGetValue (iface, out var types) ? types.Count : 0;
		}
	}
}
