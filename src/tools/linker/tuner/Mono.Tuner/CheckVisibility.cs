//
// CheckVisibility.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Tuner {

	public class CheckVisibility : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (assembly.Name.Name == "mscorlib" || assembly.Name.Name == "smcs")
				return;

			Report ("in assembly {0}", assembly.Name);

			foreach (ModuleDefinition module in assembly.Modules)
				foreach (TypeDefinition type in module.Types)
					CheckType (type);
		}

		void CheckType (TypeDefinition type)
		{
			if (!IsVisibleFrom (type, type.BaseType)) {
				Report ("Base type `{0}` of type `{1}` is not visible",
					type.BaseType, type);
			}

			CheckInterfaces (type);

			CheckFields (type);
			CheckConstructors (type);
			CheckMethods (type);
		}

		void CheckInterfaces (TypeDefinition type)
		{
			foreach (TypeReference iface in type.Interfaces) {
				if (!IsVisibleFrom (type, iface)) {
					Report ("Interface `{0}` implemented by `{1}` is not visible",
						iface, type);
				}
			}
		}

		static bool IsPublic (TypeDefinition type)
		{
			return (type.DeclaringType == null && (type.Attributes & TypeAttributes.Public) != 0) || (type.Attributes & TypeAttributes.NestedPublic) != 0;
		}

		static bool IsPublic (FieldDefinition field)
		{
			return (field.Attributes & FieldAttributes.Public) != 0;
		}

		static bool IsAssembly (FieldDefinition field)
		{
			return (field.Attributes & FieldAttributes.Assembly) != 0;
		}

		static bool IsPublic (MethodDefinition meth)
		{
			return (meth.Attributes & MethodAttributes.Public) != 0;
		}

		static bool IsAssembly (MethodDefinition meth)
		{
			return (meth.Attributes & MethodAttributes.Assem) != 0;
		}

		static bool AreInDifferentAssemblies (TypeDefinition lhs, TypeDefinition rhs)
		{
			return lhs.Module.Assembly.Name.FullName != rhs.Module.Assembly.Name.FullName;
		}

		bool IsVisibleFrom (TypeDefinition type, TypeReference reference)
		{
			if (reference == null)
				return true;

			if (reference is GenericParameter || reference.GetOriginalType () is GenericParameter)
				return true;

			TypeDefinition other = Context.Resolver.Resolve (reference);
			if (other == null)
				return true;

			if (!AreInDifferentAssemblies (type, other))
				return true;

			if (IsPublic (other))
				return true;

			return false;
		}

		bool IsVisibleFrom (TypeDefinition type, MethodReference reference)
		{
			if (reference == null)
				return true;

			MethodDefinition meth = null;
			try {
				meth = Context.Resolver.Resolve (reference);
			} catch {}

			if (meth == null)
				return true;

			TypeDefinition dec = (TypeDefinition) meth.DeclaringType;
			if (!IsVisibleFrom (type, dec))
				return false;

			if (IsPublic (meth))
				return true;

			if (!AreInDifferentAssemblies (type, dec) && IsAssembly (meth))
				return true;

			return false;
		}

		bool IsVisibleFrom (TypeDefinition type, FieldReference reference)
		{
			if (reference == null)
				return true;

			FieldDefinition field = null;
			try {
				field = Context.Resolver.Resolve (reference);
			} catch {}

			if (field == null)
				return true;

			TypeDefinition dec = (TypeDefinition) field.DeclaringType;
			if (!IsVisibleFrom (type, dec))
				return false;

			if (IsPublic (field))
				return true;

			if (!AreInDifferentAssemblies (type, dec) && IsAssembly (field))
				return true;

			return false;
		}

		void Report (string pattern, params object [] parameters)
		{
			Console.WriteLine ("[check] " + pattern, parameters);
		}

		void CheckFields (TypeDefinition type)
		{
			foreach (FieldDefinition field in type.Fields) {
				if (!IsVisibleFrom (type, field.FieldType)) {
					Report ("Field `{0}` of type `{1}` is not visible from `{2}`",
						field.Name, field.FieldType, type);
				}
			}
		}

		void CheckConstructors (TypeDefinition type)
		{
			CheckMethods (type, type.Constructors);
		}

		void CheckMethods (TypeDefinition type)
		{
			CheckMethods (type, type.Methods);
		}

		void CheckMethods (TypeDefinition type, ICollection methods)
		{
			foreach (MethodDefinition method in methods) {
				if (!IsVisibleFrom (type, method.ReturnType.ReturnType)) {
					Report ("Method return type `{0}` in method `{1}` is not visible",
						method.ReturnType.ReturnType, method);
				}

				foreach (ParameterDefinition parameter in method.Parameters) {
					if (!IsVisibleFrom (type, parameter.ParameterType)) {
						Report ("Parameter `{0}` of type `{1}` in method `{2}` is not visible.",
							parameter.Sequence, parameter.ParameterType, method);
					}
				}

				if (method.HasBody)
					CheckBody (method);
			}
		}

		void CheckBody (MethodDefinition method)
		{
			TypeDefinition type = (TypeDefinition) method.DeclaringType;

			foreach (VariableDefinition variable in method.Body.Variables) {
				if (!IsVisibleFrom ((TypeDefinition) method.DeclaringType, variable.VariableType)) {
					Report ("Variable `{0}` of type `{1}` from method `{2}` is not visible",
						variable.Index, variable.VariableType, method);
				}
			}

			foreach (Instruction instr in method.Body.Instructions) {
				switch (instr.OpCode.OperandType) {
				case OperandType.InlineType:
				case OperandType.InlineMethod:
				case OperandType.InlineField:
				case OperandType.InlineTok:
					bool error = false;
					TypeReference type_ref = instr.Operand as TypeReference;
					if (type_ref != null)
						error = !IsVisibleFrom (type, type_ref);

					MethodReference meth_ref = instr.Operand as MethodReference;
					if (meth_ref != null)
						error = !IsVisibleFrom (type, meth_ref);

					FieldReference field_ref = instr.Operand as FieldReference;
					if (field_ref != null)
						error = !IsVisibleFrom (type, field_ref);

					if (error) {
						Report ("Operand `{0}` of type {1} at offset 0x{2} in method `{3}` is not visible",
							instr.Operand, instr.OpCode.OperandType, instr.Offset.ToString ("x4"), method);
					}

					break;
				default:
					continue;
				}
			}
		}
	}
}
