// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TLens.Analyzers
{
	sealed class RedundantFieldInitializationAnalyzer : Analyzer
	{
		readonly Dictionary<MethodDefinition, List<FieldDefinition>> ctors = new Dictionary<MethodDefinition, List<FieldDefinition>> ();

		protected override void ProcessMethod (MethodDefinition method)
		{
			switch (method.Name) {
			case ".ctor":
				RedundantInitializationToDefaultValues (method);
				break;

			case ".cctor":
				RedundantInitializationToDefaultValues (method);
				break;
			}
		}

		void RedundantInitializationToDefaultValues (MethodDefinition ctor)
		{
			if (ctor.DeclaringType.IsValueType)
				return;

			var instrs = ctor.Body.Instructions;

			foreach (var instr in instrs) {
				switch (instr.OpCode.Code) {
				case Code.Stsfld:
				case Code.Stfld:
					FieldReference field = (FieldReference) instr.Operand;

					switch (field.FieldType.MetadataType) {
					case MetadataType.Boolean:
					case MetadataType.Byte:
					case MetadataType.Char:
					case MetadataType.Double:
					case MetadataType.Int16:
					case MetadataType.Int32:
					case MetadataType.Int64:
					case MetadataType.SByte:
					case MetadataType.Single:
					case MetadataType.UInt16:
					case MetadataType.UInt32:
					case MetadataType.UInt64:
						if (!IsDefaultNumeric (instr.Previous))
							continue;

						break;

					case MetadataType.String:
					case MetadataType.Class:
					case MetadataType.Object:
						if (instr.Previous.OpCode.Code != Code.Ldnull)
							continue;

						break;

					case MetadataType.Pointer:
						if (instr.Previous.OpCode.Code != Code.Conv_U || instr.Previous.Previous.OpCode.Code != Code.Ldc_I4_0)
							continue;

						break;

					case MetadataType.UIntPtr:
					case MetadataType.IntPtr:
						if (!IsLoadIntPtrOrUIntPtrZero (instr.Previous))
							continue;

						break;

					default:
						continue;
					}

					if (!ctors.TryGetValue (ctor, out var existing)) {
						existing = new List<FieldDefinition> ();
						ctors.Add (ctor, existing);
					}

					existing.Add (field.Resolve ());
					break;
				}
			}
		}

		static bool IsDefaultNumeric (Instruction instruction)
		{
			return instruction.OpCode.Code == Code.Ldc_I4_0;
		}

		static bool IsLoadIntPtrOrUIntPtrZero (Instruction instruction)
		{
			if (instruction.OpCode.Code != Code.Ldsfld)
				return false;

			if (instruction.Operand is not FieldReference fr || fr == null)
				return false;

			if (fr.DeclaringType.FullName != "System.IntPtr" && fr.DeclaringType.FullName != "System.UIntPtr")
				return false;

			return fr.Name == "Zero";
		}

		public override void PrintResults (int maxCount)
		{
			var entries = ctors.OrderByDescending (l => l.Value.Count).Take (maxCount);
			if (!entries.Any ())
				return;

			PrintHeader ("Possibly redundant fields initializations");

			foreach (var entry in entries) {
				Console.WriteLine ($"Constructor '{entry.Key.ToDisplay ()} initializes with default value fields");
				foreach (var field in entry.Value)
					Console.WriteLine ($"\tField {field.FullName}");

				Console.WriteLine ();
			}
		}
	}
}
