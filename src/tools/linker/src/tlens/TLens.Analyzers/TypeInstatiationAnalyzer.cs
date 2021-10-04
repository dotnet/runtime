// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	class TypeInstatiationAnalyzer : Analyzer
	{
		readonly Dictionary<TypeDefinition, List<MethodDefinition>> types = new Dictionary<TypeDefinition, List<MethodDefinition>> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			var instrs = method.Body.Instructions;

			foreach (var instr in instrs) {
				switch (instr.OpCode.Code) {
				case Code.Call:
				case Code.Newobj:
					if (instr.Operand is not MethodReference mr)
						throw new NotImplementedException ();

					var md = mr.Resolve ();
					if (!md.IsConstructor)
						continue;

					if (md.IsStatic)
						throw new NotImplementedException ();

					var type = md.DeclaringType;

					// Not interested in ctors chaining
					if (type == method.DeclaringType)
						continue;

					// Not interested in base class initialization
					if (md.DeclaringType == method.DeclaringType.BaseType)
						continue;

					if (!types.TryGetValue (type, out var existing)) {
						existing = new List<MethodDefinition> ();
						types.Add (type, existing);
					}

					existing.Add (method);
					break;
				}
			}
		}

		public override void PrintResults (int maxCount)
		{
			var entries = types.OrderBy (l => l.Value.Count).ThenByDescending (l => l.Key.GetEstimatedSize ()).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Limited type instantiations");

			foreach (var entry in entries) {
				Console.WriteLine ($"Type '{entry.Key.FullName}' [size: {entry.Key.GetEstimatedSize ()}] is instantiated only at");
				foreach (var values in entry.Value) {
					Console.WriteLine ($"\t{values.ToDisplay ()}");
				}

				Console.WriteLine ();
			}
		}
	}
}