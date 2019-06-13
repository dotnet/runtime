//
// Copyright (C) 2015 Xamarin, Inc (http://www.xamarin.com)
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

namespace System
{
	// A TypeName is wrapper around type names in display form
	// (that is, with special characters escaped).
	//
	// Note that in general if you unescape a type name, you will
	// lose information: If the type name's DisplayName is
	// Foo\+Bar+Baz (outer class ``Foo+Bar``, inner class Baz)
	// unescaping the first plus will give you (outer class Foo,
	// inner class Bar, innermost class Baz).
	//
	// The correct way to take a TypeName apart is to feed its
	// DisplayName to TypeSpec.Parse()
	//
	interface TypeName : IEquatable<TypeName>
	{
		string DisplayName {
			get;
		}

		// add a nested name under this one.
		TypeName NestedName (TypeIdentifier innerName);
	}

	// A type identifier is a single component of a type name.
	// Unlike a general typename, a type identifier can be be
	// converted to internal form without loss of information.
	interface TypeIdentifier : TypeName
	{
		string InternalName {
			get;
		}
	}

	static class TypeNames
	{
		internal static TypeName FromDisplay (string displayName)
		{
			return new Display (displayName);
		}

		internal abstract class ATypeName : TypeName
		{
			public abstract string DisplayName { get; }

			public abstract TypeName NestedName (TypeIdentifier innerName);

			public bool Equals (TypeName other)
			{
				return other != null && DisplayName == other.DisplayName;
			}

			public override int GetHashCode ()
			{
				return DisplayName.GetHashCode ();
			}

			public override bool Equals (object? other)
			{
				return Equals (other as TypeName);
			}
		}

		private class Display : ATypeName
		{
			string displayName;

			internal Display (string displayName)
			{
				this.displayName = displayName;
			}

			public override string DisplayName { get { return displayName; } }

			public override TypeName NestedName (TypeIdentifier innerName)
			{
				return new Display (DisplayName + "+" + innerName.DisplayName);
			}

		}
	}

	static class TypeIdentifiers
	{
		internal static TypeIdentifier FromDisplay (string displayName)
		{
			return new Display (displayName);
		}

		internal static TypeIdentifier FromInternal (string internalName)
		{
			return new Internal (internalName);
		}

		internal static TypeIdentifier FromInternal (string internalNameSpace, TypeIdentifier typeName)
		{
			return new Internal (internalNameSpace, typeName);
		}

		// Only use if simpleName is certain not to contain
		// unexpected characters that ordinarily require
		// escaping: ,+*&[]\
		internal static TypeIdentifier WithoutEscape (string simpleName)
		{
			return new NoEscape (simpleName);
		}

		private class Display : TypeNames.ATypeName, TypeIdentifier
		{
			string displayName;
			string internal_name; //cached

			internal Display (string displayName)
			{
				this.displayName = displayName;
			}

			public override string DisplayName {
				get { return displayName; }
			}

			public string InternalName {
				get {
					if (internal_name == null)
						internal_name = GetInternalName ();
					return internal_name;
				}
			}

			private string GetInternalName ()
			{
				return TypeSpec.UnescapeInternalName (displayName);
			}

			public override TypeName NestedName (TypeIdentifier innerName)
			{
				return TypeNames.FromDisplay (DisplayName + "+" + innerName.DisplayName);
			}
		}


		private class Internal : TypeNames.ATypeName, TypeIdentifier
		{
			string internalName;
			string display_name; //cached

			internal Internal (string internalName)
			{
				this.internalName = internalName;
			}

			internal Internal (string nameSpaceInternal, TypeIdentifier typeName)
			{
				this.internalName = nameSpaceInternal + "." + typeName.InternalName;
			}

			public override string DisplayName {
				get {
					if (display_name == null)
						display_name = GetDisplayName ();
					return display_name;
				}
			}

			public string InternalName {
				get { return internalName; }
			}

			private string GetDisplayName ()
			{
				return TypeSpec.EscapeDisplayName (internalName);
			}

			public override TypeName NestedName (TypeIdentifier innerName)
			{
				return TypeNames.FromDisplay (DisplayName + "+" + innerName.DisplayName);
			}

		}

		private class NoEscape : TypeNames.ATypeName, TypeIdentifier
		{
			string simpleName;
			internal NoEscape (string simpleName)
			{
				this.simpleName = simpleName;
			}

			public override string DisplayName { get { return simpleName; } }
			public string InternalName { get { return simpleName; } }

			public override TypeName NestedName (TypeIdentifier innerName)
			{
				return TypeNames.FromDisplay (DisplayName + "+" + innerName.DisplayName);
			}
		}
	}
}
