// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class UnnecessaryFieldsAssignmentAnalyzer : Analyzer
	{
		[Flags]
		enum Access
		{
			None = 0,
			Read = 1 << 1,
			Write = 1 << 2,
			ValuePropagation = 1 << 3,
		}

		readonly Dictionary<FieldDefinition, Access> fields = new Dictionary<FieldDefinition, Access> ();
		readonly Dictionary<FieldDefinition, List<MethodDefinition>> writes = new Dictionary<FieldDefinition, List<MethodDefinition>> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			if (!method.HasBody)
				return;

			foreach (var instr in method.Body.Instructions) {
				Access access;
				switch (instr.OpCode.Code) {
				case Code.Ldsfld:
				case Code.Ldfld:
					access = Access.Read;
					break;
				case Code.Ldsflda:
				case Code.Ldflda:
					access = Access.Read | Access.Write;
					break;
				case Code.Stsfld:
				case Code.Stfld:
					access = Access.Write;

					switch (instr.Previous.OpCode.Code) {
					case Code.Ldarg_0 when method.IsStatic:
					case Code.Ldarg:
					case Code.Ldarg_1:
					case Code.Ldarg_2:
					case Code.Ldarg_3:
					case Code.Ldarg_S:
						access |= Access.ValuePropagation;
						break;
					}
					break;
				default:
					continue;
				}

				var reference = (FieldReference) instr.Operand;
				FieldDefinition field = reference.Resolve ();
				if (field == null)
					continue;

				if (access == Access.Write) {
					if (!writes.TryGetValue (field, out var methods)) {
						methods = new List<MethodDefinition> ();
						writes.Add (field, methods);
					}

					methods.Add (method);
				}

				if (!fields.ContainsKey (field))
					fields.Add (field, access);
				else
					fields[field] |= access;
			}
		}

		public override void PrintResults (int maxCount)
		{
			var static_entries = fields.Where (l => l.Value == Access.Write && l.Key.IsStatic).
				OrderBy (l => l.Key, new FieldTypeSizeComparer ()).
				ThenBy (l => l.Key.DeclaringType.FullName).
				Take (maxCount);
			if (static_entries.Any ()) {
				PrintHeader ("Unnecessary static fields assignments");

				foreach (var entry in static_entries) {
					Console.WriteLine ($"Field '{entry.Key.FullName}' is never read but has value assigned");

					foreach (var loc in writes[entry.Key]) {
						Console.WriteLine ($"\t{loc.ToDisplay ()}");
					}

					Console.WriteLine ();
				}
			}

			var instance_entries = fields.Where (l => l.Value == Access.Write && !l.Key.IsStatic).
				OrderBy (l => l.Key, new FieldTypeSizeComparer ()).
				ThenBy (l => l.Key.DeclaringType.FullName).
				Take (maxCount);
			if (instance_entries.Any ()) {
				PrintHeader ("Unnecessary instance fields assignments");

				foreach (var entry in instance_entries) {
					Console.WriteLine ($"Field '{entry.Key.FullName}' is never read but has value assigned");

					foreach (var loc in writes[entry.Key]) {
						Console.WriteLine ($"\t{loc.ToDisplay ()}");
					}

					Console.WriteLine ();
				}
			}
		}

		struct FieldTypeSizeComparer : IComparer<FieldDefinition>
		{
			public int Compare (FieldDefinition x, FieldDefinition y)
			{
				var x_type = x.FieldType;
				var y_type = y.FieldType;

				return GetSize (y_type).CompareTo (GetSize (x_type));
			}

			static int GetSize (TypeReference type)
			{
				if (type.IsArray)
					return 100;

				if (type.MetadataType == MetadataType.String)
					return 50;

				if (type.IsPrimitive || type.IsPointer)
					return 1;

				return 10;
			}
		}
	}
}
