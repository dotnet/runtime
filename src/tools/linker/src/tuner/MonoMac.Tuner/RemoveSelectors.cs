using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Mono.Tuner;

namespace MonoMac.Tuner {

	public class RemoveSelectors : IStep {

		public void Process (LinkContext context)
		{
			AssemblyDefinition monomac;
			if (!context.TryGetLinkedAssembly ("MonoMac", out monomac))
				return;

			foreach (TypeDefinition type in monomac.MainModule.Types) {
				if (!type.IsNSObject ())
					continue;

				ProcessNSObject (type);
			}
		}

		static void ProcessNSObject (TypeDefinition type)
		{
			var selectors = PopulateSelectors (type);
			if (selectors == null)
				return;

			foreach (var method in CollectMethods (type))
				CheckSelectorUsage (method, selectors);

			if (selectors.Count == 0)
				return;

			PatchStaticConstructor (type, selectors);
			RemoveUnusedSelectors (type, selectors);
		}

		static void CheckSelectorUsage (MethodDefinition method, HashSet<FieldDefinition> selectors)
		{
			if (!method.HasBody)
				return;

			foreach (Instruction instruction in method.Body.Instructions) {
				switch (instruction.OpCode.OperandType) {
				case OperandType.InlineTok:
				case OperandType.InlineField:
					var field = instruction.Operand as FieldDefinition;
					if (field == null)
						continue;

					if (selectors.Contains (field))
						selectors.Remove (field);

					break;
				}
			}
		}

		static void PatchStaticConstructor (TypeDefinition type, HashSet<FieldDefinition> selectors)
		{
			var cctor = type.GetTypeConstructor ();
			if (cctor == null || !cctor.HasBody)
				return;

			var instructions = cctor.Body.Instructions;

			for (int i = 0; i < instructions.Count; i++) {
				var instruction = instructions [i];
				if (!IsCreateSelector (instruction, selectors))
					continue;

				instructions.RemoveAt (i--);
				instructions.RemoveAt (i--);
				instructions.RemoveAt (i--);
			}
		}

		static bool IsCreateSelector (Instruction instruction, HashSet<FieldDefinition> selectors)
		{
			if (instruction.OpCode != OpCodes.Stsfld)
				return false;

			var field = instruction.Operand as FieldDefinition;
			if (field == null)
				return false;

			if (!selectors.Contains (field))
				return false;

			instruction = instruction.Previous;
			if (instruction == null)
				return false;

			if (instruction.OpCode != OpCodes.Call)
				return false;

			if (!IsRegisterSelector (instruction.Operand as MethodReference))
				return false;

			instruction = instruction.Previous;
			if (instruction == null)
				return false;

			if (instruction.OpCode != OpCodes.Ldstr)
				return false;

			return true;
		}

		static bool IsRegisterSelector (MethodReference method)
		{
			if (method == null)
				return false;

			if (method.Name != "GetHandle" && method.Name != "sel_registerName")
				return false;

			if (method.DeclaringType.FullName != "MonoMac.ObjCRuntime.Selector")
				return false;

			return true;
		}

		static void RemoveUnusedSelectors (TypeDefinition type, HashSet<FieldDefinition> selectors)
		{
			var fields = type.Fields;

			for (int i = 0; i < fields.Count; i++)
				if (selectors.Contains (fields [i]))
					fields.RemoveAt (i--);
		}

		static HashSet<FieldDefinition> PopulateSelectors (TypeDefinition type)
		{
			if (!type.HasFields)
				return null;

			HashSet<FieldDefinition> selectors = null;

			foreach (FieldDefinition field in type.Fields) {
				if (!IsSelector (field))
					continue;

				if (selectors == null)
					selectors = new HashSet<FieldDefinition> ();

				selectors.Add (field);
			}

			return selectors;
		}

		static bool IsSelector (FieldDefinition field)
		{
			if (!field.IsStatic)
				return false;

			if (field.FieldType.FullName != "System.IntPtr")
				return false;

			if (!field.Name.StartsWith ("sel"))
				return false;

			return true;
		}

		static IEnumerable<MethodDefinition> CollectMethods (TypeDefinition type)
		{
			if (!type.HasMethods)
				yield break;

			foreach (MethodDefinition method in type.Methods) {
				if (method.IsStatic && method.IsConstructor)
					continue;

				yield return method;
			}
		}
	}
}
