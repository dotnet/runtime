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
using System.Text;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Tuner {

	public class CheckVisibility : BaseStep {

		bool throw_on_error;

		protected override void Process ()
		{
			throw_on_error = GetThrowOnVisibilityErrorParameter ();
		}

		bool GetThrowOnVisibilityErrorParameter ()
		{
			try {
				return bool.Parse (Context.GetParameter ("throw_on_visibility_error"));
			} catch {
				return false;
			}
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (assembly.Name.Name == "mscorlib" || assembly.Name.Name == "smcs")
				return;

			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			Report ("in assembly {0}", assembly.Name);

			foreach (ModuleDefinition module in assembly.Modules)
				foreach (TypeDefinition type in module.Types)
					CheckType (type);
		}

		void CheckType (TypeDefinition type)
		{
			if (!IsVisibleFrom (type, type.BaseType)) {
				ReportError ("Base type `{0}` of type `{1}` is not visible",
					type.BaseType, type);
			}

			CheckInterfaces (type);

			CheckFields (type);
			CheckMethods (type);
		}

		void CheckInterfaces (TypeDefinition type)
		{
			foreach (TypeReference iface in type.Interfaces) {
				if (!IsVisibleFrom (type, iface)) {
					ReportError ("Interface `{0}` implemented by `{1}` is not visible",
						iface, type);
				}
			}
		}

		static bool IsPublic (TypeDefinition type)
		{
			return (type.DeclaringType == null && type.IsPublic) || type.IsNestedPublic;
		}

		static bool AreInDifferentAssemblies (TypeDefinition type, TypeDefinition target)
		{
			if (type.Module.Assembly.Name.FullName == target.Module.Assembly.Name.FullName)
				return false;

			return !IsInternalVisibleTo (target.Module.Assembly, type.Module.Assembly);
		}

		static bool IsInternalVisibleTo (AssemblyDefinition assembly, AssemblyDefinition candidate)
		{
			foreach (CustomAttribute attribute in assembly.CustomAttributes) {
				if (!IsInternalsVisibleToAttribute (attribute))
					continue;

				if (attribute.ConstructorArguments.Count == 0)
					continue;

				string signature = (string) attribute.ConstructorArguments [0].Value;

				if (InternalsVisibleToSignatureMatch (signature, candidate.Name))
					return true;
			}

			return false;
		}

		static bool InternalsVisibleToSignatureMatch (string signature, AssemblyNameReference reference)
		{
			int pos = signature.IndexOf (",");
			if (pos == -1)
				return signature == reference.Name;

			string assembly_name = signature.Substring (0, pos);

			pos = signature.IndexOf ("=");
			if (pos == -1)
				throw new ArgumentException ();

			string public_key = signature.Substring (pos + 1).ToLower ();

			return assembly_name == reference.Name && public_key == ToPublicKeyString (reference.PublicKey);
		}

		static string ToPublicKeyString (byte [] public_key)
		{
			StringBuilder signature = new StringBuilder (public_key.Length);
			for (int i = 0; i < public_key.Length; i++)
				signature.Append (public_key [i].ToString ("x2"));

			return signature.ToString ();
		}

		static bool IsInternalsVisibleToAttribute (CustomAttribute attribute)
		{
			return attribute.Constructor.DeclaringType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute";
		}

		bool IsVisibleFrom (TypeDefinition type, TypeReference reference)
		{
			if (reference == null)
				return true;

			if (reference is GenericParameter || reference.GetElementType () is GenericParameter)
				return true;

			TypeDefinition other = reference.Resolve ();
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

			MethodDefinition meth = reference.Resolve ();
			if (meth == null)
				return true;

			TypeDefinition dec = (TypeDefinition) meth.DeclaringType;
			if (!IsVisibleFrom (type, dec))
				return false;

			if (meth.IsPublic)
				return true;

			if (type == dec || IsNestedIn (type, dec))
				return true;

			if (meth.IsFamily && InHierarchy (type, dec))
				return true;

			if (meth.IsFamilyOrAssembly && (!AreInDifferentAssemblies (type, dec) || InHierarchy (type, dec)))
				return true;

			if (meth.IsFamilyAndAssembly && (!AreInDifferentAssemblies (type, dec) && InHierarchy (type, dec)))
				return true;

			if (!AreInDifferentAssemblies (type, dec) && meth.IsAssembly)
				return true;

			return false;
		}

		bool IsVisibleFrom (TypeDefinition type, FieldReference reference)
		{
			if (reference == null)
				return true;

			FieldDefinition field = reference.Resolve ();
			if (field == null)
				return true;

			TypeDefinition dec = (TypeDefinition) field.DeclaringType;
			if (!IsVisibleFrom (type, dec))
				return false;

			if (field.IsPublic)
				return true;

			if (type == dec || IsNestedIn (type, dec))
				return true;

			if (field.IsFamily && InHierarchy (type, dec))
				return true;

			if (field.IsFamilyOrAssembly && (!AreInDifferentAssemblies (type, dec) || InHierarchy (type, dec)))
				return true;

			if (field.IsFamilyAndAssembly && (!AreInDifferentAssemblies (type, dec) && InHierarchy (type, dec)))
				return true;

			if (!AreInDifferentAssemblies (type, dec) && field.IsAssembly)
				return true;

			return false;
		}

		static bool IsNestedIn (TypeDefinition type, TypeDefinition other)
		{
			TypeDefinition declaring = type.DeclaringType;

			if (declaring == null)
				return false;

			if (declaring == other)
				return true;

			if (declaring.DeclaringType == null)
				return false;

			return IsNestedIn (declaring, other);
		}

		static bool InHierarchy (TypeDefinition type, TypeDefinition other)
		{
			if (type.BaseType == null)
				return false;

			TypeDefinition baseType = type.BaseType.Resolve ();

			if (baseType == other)
				return true;

			return InHierarchy (baseType, other);
		}

		static void Report (string pattern, params object [] parameters)
		{
			Console.WriteLine ("[check] " + pattern, parameters);
		}

		void ReportError (string pattern, params object [] parameters)
		{
			Report (pattern, parameters);

			if (throw_on_error)
				throw new VisibilityErrorException (string.Format (pattern, parameters));
		}

		void CheckFields (TypeDefinition type)
		{
			foreach (FieldDefinition field in type.Fields) {
				if (!IsVisibleFrom (type, field.FieldType)) {
					ReportError ("Field `{0}` of type `{1}` is not visible from `{2}`",
						field.Name, field.FieldType, type);
				}
			}
		}

		void CheckMethods (TypeDefinition type)
		{
			CheckMethods (type, type.Methods);
		}

		void CheckMethods (TypeDefinition type, ICollection methods)
		{
			foreach (MethodDefinition method in methods) {
				if (!IsVisibleFrom (type, method.ReturnType)) {
					ReportError ("Method return type `{0}` in method `{1}` is not visible",
						method.ReturnType, method);
				}

				foreach (ParameterDefinition parameter in method.Parameters) {
					if (!IsVisibleFrom (type, parameter.ParameterType)) {
						ReportError ("Parameter `{0}` of type `{1}` in method `{2}` is not visible.",
							parameter.Index, parameter.ParameterType, method);
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
					ReportError ("Variable `{0}` of type `{1}` from method `{2}` is not visible",
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
						ReportError ("Operand `{0}` of type {1} at offset 0x{2} in method `{3}` is not visible",
							instr.Operand, instr.OpCode.OperandType, instr.Offset.ToString ("x4"), method);
					}

					break;
				default:
					continue;
				}
			}
		}

		class VisibilityErrorException : Exception {

			public VisibilityErrorException (string message)
				: base (message)
			{
			}
		}
	}
}
