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

using System.Collections.Generic;

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class TypeMapStep : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (TypeDefinition type in assembly.MainModule.Types)
				MapType (type);
		}

		protected virtual void MapType (TypeDefinition type)
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

			foreach (var @interface in type.Interfaces) {
				var iface = @interface.InterfaceType.Resolve ();
				if (iface == null || !iface.HasMethods)
					continue;

				foreach (MethodDefinition method in iface.Methods) {
					if (TryMatchMethod (type, method) != null)
						continue;

					var @base = GetBaseMethodInTypeHierarchy (type, method);
					if (@base == null)
						continue;

					AnnotateMethods (method, @base);
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
			foreach (MethodDefinition @base in GetBaseMethodsInInterfaceHierarchy (method))
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

		static IEnumerable<MethodDefinition> GetBaseMethodsInInterfaceHierarchy (MethodDefinition method)
		{
			return GetBaseMethodsInInterfaceHierarchy (method.DeclaringType, method);
		}

		static IEnumerable<MethodDefinition> GetBaseMethodsInInterfaceHierarchy (TypeDefinition type, MethodDefinition method)
		{
			if (!type.HasInterfaces)
				yield break;

			foreach (var interface_ref in type.Interfaces) {
				TypeDefinition @interface = interface_ref.InterfaceType.Resolve ();
				if (@interface == null)
					continue;

				MethodDefinition base_method = TryMatchMethod (@interface, method);
				if (base_method != null)
					yield return base_method;

				foreach (MethodDefinition @base in GetBaseMethodsInInterfaceHierarchy (@interface, method))
					yield return @base;
			}
		}

		static MethodDefinition TryMatchMethod (TypeDefinition type, MethodDefinition method)
		{
			if (!type.HasMethods)
				return null;

			Dictionary<string,string> gp = null;
			foreach (MethodDefinition candidate in type.Methods) {
				if (MethodMatch (candidate, method, ref gp))
					return candidate;
				if (gp != null)
					gp.Clear ();
			}

			return null;
		}

		static bool MethodMatch (MethodDefinition candidate, MethodDefinition method, ref Dictionary<string,string> genericParameters)
		{
			if (!candidate.IsVirtual)
				return false;

			if (candidate.HasParameters != method.HasParameters)
				return false;

			if (candidate.Name != method.Name)
				return false;

			if (candidate.HasGenericParameters != method.HasGenericParameters)
				return false;

			// we need to track what the generic parameter represent - as we cannot allow it to
			// differ between the return type or any parameter
			if (!TypeMatch (candidate.ReturnType, method.ReturnType, ref genericParameters))
				return false;

			if (!candidate.HasParameters)
				return true;

			var cp = candidate.Parameters;
			var mp = method.Parameters;
			if (cp.Count != mp.Count)
				return false;

			for (int i = 0; i < cp.Count; i++) {
				if (!TypeMatch (cp [i].ParameterType, mp [i].ParameterType, ref genericParameters))
					return false;
			}

			return true;
		}

		static bool TypeMatch (IModifierType a, IModifierType b, ref Dictionary<string,string> gp)
		{
			if (!TypeMatch (a.ModifierType, b.ModifierType, ref gp))
				return false;

			return TypeMatch (a.ElementType, b.ElementType, ref gp);
		}

		static bool TypeMatch (TypeSpecification a, TypeSpecification b, ref Dictionary<string,string> gp)
		{
			var gita = a as GenericInstanceType;
			if (gita != null)
				return TypeMatch (gita, (GenericInstanceType) b, ref gp);

			var mta = a as IModifierType;
			if (mta != null)
				return TypeMatch (mta, (IModifierType) b, ref gp);

			return TypeMatch (a.ElementType, b.ElementType, ref gp);
		}

		static bool TypeMatch (GenericInstanceType a, GenericInstanceType b, ref Dictionary<string,string> gp)
		{
			if (!TypeMatch (a.ElementType, b.ElementType, ref gp))
				return false;

			if (a.HasGenericArguments != b.HasGenericArguments)
				return false;

			if (!a.HasGenericArguments)
				return true;

			var gaa = a.GenericArguments;
			var gab = b.GenericArguments;
			if (gaa.Count != gab.Count)
				return false;

			for (int i = 0; i < gaa.Count; i++) {
				if (!TypeMatch (gaa [i], gab [i], ref gp))
					return false;
			}

			return true;
		}

		static bool TypeMatch (TypeReference a, TypeReference b, ref Dictionary<string,string> gp)
		{
			var gpa = a as GenericParameter;
			if (gpa != null) {
				if (gp == null)
					gp = new Dictionary<string, string> ();
				string match;
				if (!gp.TryGetValue (gpa.FullName, out match)) {
					// first use, we assume it will always be used this way
					gp.Add (gpa.FullName, b.ToString ());
					return true;
				}
				// re-use, it should match the previous usage
				return match == b.ToString ();
			}

			if (a is TypeSpecification || b is TypeSpecification) {
				if (a.GetType () != b.GetType ())
					return false;

				return TypeMatch ((TypeSpecification) a, (TypeSpecification) b, ref gp);
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
