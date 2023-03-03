// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection.Runtime.TypeParsing
{
	//
	// The TypeName class is the base class for a family of types that represent the nodes in a parse tree for 
	// assembly-qualified type names.
	//
	public abstract class TypeName
	{
		public abstract override string ToString();
	}

	//
	// Represents a parse of a type name optionally qualified by an assembly name. If present, the assembly name follows
	// a comma following the type name.
	//
	public sealed class AssemblyQualifiedTypeName : TypeName
	{
		public AssemblyQualifiedTypeName(NonQualifiedTypeName typeName, RuntimeAssemblyName assemblyName)
		{
			Debug.Assert(typeName != null);
			TypeName = typeName;
			AssemblyName = assemblyName;
		}

		public sealed override string ToString()
		{
			return TypeName.ToString() + ((AssemblyName == null) ? "" : ", " + AssemblyName.FullName);
		}

		public RuntimeAssemblyName AssemblyName { get; }
		public NonQualifiedTypeName TypeName { get; }
	}

	//
	// Base class for all non-assembly-qualified type names.
	//
	public abstract class NonQualifiedTypeName : TypeName
	{
	}

	//
	// Base class for namespace or nested type.
	//
	internal abstract class NamedTypeName : NonQualifiedTypeName
	{
	}

	//
	// Non-nested named type. The full name is the namespace-qualified name. For example, the FullName for
	// System.Collections.Generic.IList<> is "System.Collections.Generic.IList`1".
	//
	internal sealed partial class NamespaceTypeName : NamedTypeName
	{
		public NamespaceTypeName(string[] namespaceParts, string name)
		{
			Debug.Assert(namespaceParts != null);
			Debug.Assert(name != null);

			_name = name;
			_namespaceParts = namespaceParts;
		}

		public sealed override string ToString()
		{
			string fullName = "";
			for (int i = 0; i < _namespaceParts.Length; i++)
			{
				fullName += _namespaceParts[_namespaceParts.Length - i - 1];
				fullName += ".";
			}
			fullName += _name;
			return fullName;
		}

		private string _name;
		private string[] _namespaceParts;
	}

	//
	// A nested type. The Name is the simple name of the type (not including any portion of its declaring type name.)
	//
	internal sealed class NestedTypeName : NamedTypeName
	{
		public NestedTypeName(string name, NamedTypeName declaringType)
		{
			Name = name;
			DeclaringType = declaringType;
		}

		public string Name { get; private set; }
		public NamedTypeName DeclaringType { get; private set; }

		public sealed override string ToString()
		{
			// Cecil's format uses '/' instead of '+' for nested types.
			return DeclaringType + "/" + Name;
		}
	}

	//
	// Abstract base for array, byref and pointer type names.
	//
	internal abstract class HasElementTypeName : NonQualifiedTypeName
	{
		public HasElementTypeName(TypeName elementTypeName)
		{
			ElementTypeName = elementTypeName;
		}

		public TypeName ElementTypeName { get; }
	}

	//
	// A single-dimensional zero-lower-bound array type name.
	//
	internal sealed class ArrayTypeName : HasElementTypeName
	{
		public ArrayTypeName(TypeName elementTypeName)
			: base(elementTypeName)
		{
		}

		public sealed override string ToString()
		{
			return ElementTypeName + "[]";
		}
	}

	//
	// A multidim array type name.
	//
	internal sealed class MultiDimArrayTypeName : HasElementTypeName
	{
		public MultiDimArrayTypeName(TypeName elementTypeName, int rank)
			: base(elementTypeName)
		{
			Rank = rank;
		}

		public sealed override string ToString()
		{
			return ElementTypeName + "[" + (Rank == 1 ? "*" : new string(',', Rank - 1)) + "]";
		}

		public int Rank { get; }
	}

	//
	// A byref type.
	//
	internal sealed class ByRefTypeName : HasElementTypeName
	{
		public ByRefTypeName(TypeName elementTypeName)
			: base(elementTypeName)
		{
		}

		public sealed override string ToString()
		{
			return ElementTypeName + "&";
		}
	}

	//
	// A pointer type.
	//
	internal sealed class PointerTypeName : HasElementTypeName
	{
		public PointerTypeName(TypeName elementTypeName)
			: base(elementTypeName)
		{
		}

		public sealed override string ToString()
		{
			return ElementTypeName + "*";
		}
	}

	//
	// A constructed generic type.
	//
	internal sealed class ConstructedGenericTypeName : NonQualifiedTypeName
	{
		public ConstructedGenericTypeName(NamedTypeName genericType, IEnumerable<TypeName> genericArguments)
		{
			GenericType = genericType;
			GenericArguments = genericArguments;
		}

		public NamedTypeName GenericType { get; }
		public IEnumerable<TypeName> GenericArguments { get; }

		public sealed override string ToString()
		{
			string s = GenericType.ToString();
			s += "[";
			string sep = "";
			foreach (TypeName genericTypeArgument in GenericArguments)
			{
				s += sep;
				sep = ",";
				AssemblyQualifiedTypeName assemblyQualifiedTypeArgument = genericTypeArgument as AssemblyQualifiedTypeName;
				if (assemblyQualifiedTypeArgument == null || assemblyQualifiedTypeArgument.AssemblyName == null)
					s += genericTypeArgument.ToString();
				else
					s += "[" + genericTypeArgument.ToString() + "]";
			}
			s += "]";
			return s;
		}
	}
}