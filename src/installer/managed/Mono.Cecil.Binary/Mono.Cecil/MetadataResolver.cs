//
// MetadataResolver.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2008 Jb Evain (http://evain.net)
// (C) 2008 Novell, Inc. (http://www.novell.com)
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

namespace Mono.Cecil {

	class MetadataResolver {

		AssemblyDefinition assembly;

		public IAssemblyResolver AssemblyResolver {
			get { return assembly.Resolver; }
		}

		public MetadataResolver (AssemblyDefinition assembly)
		{
			this.assembly = assembly;
		}

		public TypeDefinition Resolve (TypeReference type)
		{
			type = type.GetOriginalType ();

			if (type is TypeDefinition)
				return (TypeDefinition) type;

			AssemblyNameReference reference = type.Scope as AssemblyNameReference;
			if (reference != null) {
				AssemblyDefinition assembly = AssemblyResolver.Resolve (reference);
				if (assembly == null)
					return null;

				return assembly.MainModule.Types [type.FullName];
			}

			ModuleDefinition module = type.Scope as ModuleDefinition;
			if (module != null)
				return module.Types [type.FullName];

			ModuleReference mod_reference = type.Scope as ModuleReference;
			if (mod_reference != null) {
				foreach (ModuleDefinition netmodule in type.Module.Assembly.Modules)
					if (netmodule.Name == mod_reference.Name)
						return netmodule.Types [type.FullName];
			}

			throw new NotImplementedException ();
		}

		public FieldDefinition Resolve (FieldReference field)
		{
			TypeDefinition type = Resolve (field.DeclaringType);
			if (type == null)
				return null;

			return type.HasFields ? GetField (type.Fields, field) : null;
		}

		static FieldDefinition GetField (ICollection collection, FieldReference reference)
		{
			foreach (FieldDefinition field in collection) {
				if (field.Name != reference.Name)
					continue;

				if (!AreSame (field.FieldType, reference.FieldType))
					continue;

				return field;
			}

			return null;
		}

		public MethodDefinition Resolve (MethodReference method)
		{
			TypeDefinition type = Resolve (method.DeclaringType);
			if (type == null)
				return null;

			method = method.GetOriginalMethod ();
			if (method.Name == MethodDefinition.Cctor || method.Name == MethodDefinition.Ctor)
				return type.HasConstructors ? GetMethod (type.Constructors, method) : null;
			else
				return type.HasMethods ? GetMethod (type, method) : null;
		}

		MethodDefinition GetMethod (TypeDefinition type, MethodReference reference)
		{
			while (type != null) {
				MethodDefinition method = GetMethod (type.Methods, reference);
				if (method == null) {
					if (type.BaseType == null)
						return null;

					type = Resolve (type.BaseType);
				} else
					return method;
			}

			return null;
		}

		static MethodDefinition GetMethod (ICollection collection, MethodReference reference)
		{
			foreach (MethodDefinition meth in collection) {
				if (meth.Name != reference.Name)
					continue;

				if (!AreSame (meth.ReturnType.ReturnType, reference.ReturnType.ReturnType))
					continue;

				if (meth.HasParameters != reference.HasParameters)
					continue;

				if (!meth.HasParameters && !reference.HasParameters)
					return meth; //both have no parameters hence meth is the good one

				if (!AreSame (meth.Parameters, reference.Parameters))
					continue;

				return meth;
			}

			return null;
		}

		static bool AreSame (ParameterDefinitionCollection a, ParameterDefinitionCollection b)
		{
			if (a.Count != b.Count)
				return false;

			if (a.Count == 0)
				return true;

			for (int i = 0; i < a.Count; i++)
				if (!AreSame (a [i].ParameterType, b [i].ParameterType))
					return false;

			return true;
		}

		static bool AreSame (ModType a, ModType b)
		{
			if (!AreSame (a.ModifierType, b.ModifierType))
				return false;

			return AreSame (a.ElementType, b.ElementType);
		}

		static bool AreSame (TypeSpecification a, TypeSpecification b)
		{
			if (a is GenericInstanceType)
				return AreSame ((GenericInstanceType) a, (GenericInstanceType) b);

			if (a is ModType)
				return AreSame ((ModType) a, (ModType) b);

			return AreSame (a.ElementType, b.ElementType);
		}

		static bool AreSame (GenericInstanceType a, GenericInstanceType b)
		{
			if (!AreSame (a.ElementType, b.ElementType))
				return false;

			if (a.GenericArguments.Count != b.GenericArguments.Count)
				return false;

			if (a.GenericArguments.Count == 0)
				return true;

			for (int i = 0; i < a.GenericArguments.Count; i++)
				if (!AreSame (a.GenericArguments [i], b.GenericArguments [i]))
					return false;

			return true;
		}

		static bool AreSame (GenericParameter a, GenericParameter b)
		{
			return a.Position == b.Position;
		}

		static bool AreSame (TypeReference a, TypeReference b)
		{
			if (a is TypeSpecification || b is TypeSpecification) {
				if (a.GetType () != b.GetType ())
					return false;

				return AreSame ((TypeSpecification) a, (TypeSpecification) b);
			}

			if (a is GenericParameter || b is GenericParameter) {
				if (a.GetType () != b.GetType ())
					return false;

				return AreSame ((GenericParameter) a, (GenericParameter) b);
			}

			return a.FullName == b.FullName;
		}
	}
}
