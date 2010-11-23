//
// TypeMapStep.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 Novell, Inc.
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

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class TypeMapStep : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (TypeDefinition type in assembly.MainModule.Types)
				MapType (type);
		}

		void MapType (TypeDefinition type)
		{
			MapVirtualMethods (type);
			MapInterfaceMethodsInTypeHierarchy (type);

			if (!type.HasNestedTypes)
				return;

			foreach (var nested in type.NestedTypes)
				MapType (nested);
		}

		void MapInterfaceMethodsInTypeHierarchy (TypeDefinition type)
		{
			if (!type.HasInterfaces)
				return;

			foreach (TypeReference @interface in type.Interfaces) {
				var iface = @interface.Resolve ();
				if (iface == null || !iface.HasMethods)
					continue;

				foreach (MethodDefinition method in iface.Methods) {
					if (TryMatchMethod (type, method) != null)
						continue;

					var @base = GetBaseMethodInTypeHierarchy (type, method);
					if (@base == null)
						continue;

					Annotations.AddPreservedMethod (type, @base);
				}
			}
		}

		void MapVirtualMethods (TypeDefinition type)
		{
			if (!type.HasMethods)
				return;

			foreach (MethodDefinition method in type.Methods) {
				if (!method.IsVirtual)
					continue;

				MapVirtualMethod (method);

				if (method.HasOverrides)
					MapOverrides (method);
			}
		}

		void MapVirtualMethod (MethodDefinition method)
		{
			MapVirtualBaseMethod (method);
			MapVirtualInterfaceMethod (method);
		}

		void MapVirtualBaseMethod (MethodDefinition method)
		{
			MethodDefinition @base = GetBaseMethodInTypeHierarchy (method);
			if (@base == null)
				return;

			AnnotateMethods (@base, method);
		}

		void MapVirtualInterfaceMethod (MethodDefinition method)
		{
			MethodDefinition @base = GetBaseMethodInInterfaceHierarchy (method);
			if (@base == null)
				return;

			AnnotateMethods (@base, method);
		}

		void MapOverrides (MethodDefinition method)
		{
			foreach (MethodReference override_ref in method.Overrides) {
				MethodDefinition @override = override_ref.Resolve ();
				if (@override == null)
					continue;

				AnnotateMethods (@override, method);
			}
		}

		void AnnotateMethods (MethodDefinition @base, MethodDefinition @override)
		{
			Annotations.AddBaseMethod (@override, @base);
			Annotations.AddOverride (@base, @override);
		}

		static MethodDefinition GetBaseMethodInTypeHierarchy (MethodDefinition method)
		{
			return GetBaseMethodInTypeHierarchy (method.DeclaringType, method);
		}

		static MethodDefinition GetBaseMethodInTypeHierarchy (TypeDefinition type, MethodDefinition method)
		{
			TypeDefinition @base = GetBaseType (type);
			while (@base != null) {
				MethodDefinition base_method = TryMatchMethod (@base, method);
				if (base_method != null)
					return base_method;

				@base = GetBaseType (@base);
			}

			return null;
		}

		static MethodDefinition GetBaseMethodInInterfaceHierarchy (MethodDefinition method)
		{
			return GetBaseMethodInInterfaceHierarchy (method.DeclaringType, method);
		}

		static MethodDefinition GetBaseMethodInInterfaceHierarchy (TypeDefinition type, MethodDefinition method)
		{
			if (!type.HasInterfaces)
				return null;

			foreach (TypeReference interface_ref in type.Interfaces) {
				TypeDefinition @interface = interface_ref.Resolve ();
				if (@interface == null)
					continue;

				MethodDefinition base_method = TryMatchMethod (@interface, method);
				if (base_method != null)
					return base_method;

				base_method = GetBaseMethodInInterfaceHierarchy (@interface, method);
				if (base_method != null)
					return base_method;
			}

			return null;
		}

		static MethodDefinition TryMatchMethod (TypeDefinition type, MethodDefinition method)
		{
			if (!type.HasMethods)
				return null;

			foreach (MethodDefinition candidate in type.Methods)
				if (MethodMatch (candidate, method))
					return candidate;

			return null;
		}

		static bool MethodMatch (MethodDefinition candidate, MethodDefinition method)
		{
			if (!candidate.IsVirtual)
				return false;

			if (candidate.Name != method.Name)
				return false;

			if (!TypeMatch (candidate.ReturnType, method.ReturnType))
				return false;

			if (candidate.Parameters.Count != method.Parameters.Count)
				return false;

			for (int i = 0; i < candidate.Parameters.Count; i++)
				if (!TypeMatch (candidate.Parameters [i].ParameterType, method.Parameters [i].ParameterType))
					return false;

			return true;
		}

		static bool TypeMatch (IModifierType a, IModifierType b)
		{
			if (!TypeMatch (a.ModifierType, b.ModifierType))
				return false;

			return TypeMatch (a.ElementType, b.ElementType);
		}

		static bool TypeMatch (TypeSpecification a, TypeSpecification b)
		{
			if (a is GenericInstanceType)
				return TypeMatch ((GenericInstanceType) a, (GenericInstanceType) b);

			if (a is IModifierType)
				return TypeMatch ((IModifierType) a, (IModifierType) b);

			return TypeMatch (a.ElementType, b.ElementType);
		}

		static bool TypeMatch (GenericInstanceType a, GenericInstanceType b)
		{
			if (!TypeMatch (a.ElementType, b.ElementType))
				return false;

			if (a.GenericArguments.Count != b.GenericArguments.Count)
				return false;

			if (a.GenericArguments.Count == 0)
				return true;

			for (int i = 0; i < a.GenericArguments.Count; i++)
				if (!TypeMatch (a.GenericArguments [i], b.GenericArguments [i]))
					return false;

			return true;
		}

		static bool TypeMatch (TypeReference a, TypeReference b)
		{
			if (a is GenericParameter)
				return true;

			if (a is TypeSpecification || b is TypeSpecification) {
				if (a.GetType () != b.GetType ())
					return false;

				return TypeMatch ((TypeSpecification) a, (TypeSpecification) b);
			}

			return a.FullName == b.FullName;
		}

		static TypeDefinition GetBaseType (TypeDefinition type)
		{
			if (type == null || type.BaseType == null)
				return null;

			return type.BaseType.Resolve ();
		}
	}
}
