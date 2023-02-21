// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
	/// <summary>
	/// Used to indicate the index of the parameter in the Parameters metadata section (i.e. the first parameter that is not the implicit 'this' is 0)
	/// It is very error prone to use an int to represent the index in parameters metadata section / source code parameter index as well as for indexing into argument lists.
	/// Instead, use this struct whenever representing an index of a parameter.
	/// </summary>
	/// <example>
	/// In a call to a non-static function Foo(int a, int b, int c)
	/// 0 refers to 'this'
	/// 1 refers to a,
	/// 2 refers to b,
	/// 3 refers to c.
	/// In a call to a static extension function Foo(this Bar a, int b, int c)
	/// 0 refers to "this Bar a"
	/// 1 refers to b,
	/// 2 refers to c.
	/// In a call to a static function Foo(int a, int b, int c)
	/// 0 refers to a,
	/// 1 refers to b,
	/// 2 refers to c.
	/// </example>
	public readonly struct ParameterIndex : System.IEquatable<ParameterIndex>
	{
		public readonly int Index;

		public ParameterIndex (int x)
			=> Index = x;

		public static bool operator == (ParameterIndex left, ParameterIndex right) => left.Index == right.Index;

		public static bool operator != (ParameterIndex left, ParameterIndex right) => left.Index != right.Index;

		public static ParameterIndex operator ++ (ParameterIndex val) => new ParameterIndex (val.Index + 1);

		public override bool Equals ([NotNullWhen (true)] object? obj)
			=> obj is ParameterIndex other && Index == other.Index;

		public bool Equals (ParameterIndex other)
			=> Index == other.Index;

		public override int GetHashCode () => Index.GetHashCode ();

		public static explicit operator ParameterIndex (int x)
			=> new ParameterIndex (x);

		public static explicit operator int (ParameterIndex x)
			=> x.Index;
		public static ParameterIndex operator + (ParameterIndex left, ParameterIndex right)
			=> new ParameterIndex (left.Index + right.Index);

		public static ParameterIndex operator - (ParameterIndex left, ParameterIndex right)
			=> new ParameterIndex (left.Index - right.Index);

		public static ParameterIndex operator + (ParameterIndex left, int right)
			=> new ParameterIndex (left.Index + right);

		public static ParameterIndex operator - (ParameterIndex left, int right)
			=> new ParameterIndex (left.Index - right);
	}
}
