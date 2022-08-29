// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared
{
	/// <summary>
	/// Used to indicate the index of the parameter in source code (i.e. the index is not offset by 1 if there is a `this` parameter)
	/// This enum and <see cref="T:Mono.Linker.ILParameterIndex"/> is used to enforce a differentiation between scenarios where the 0 index should be `this` and when the 0 index should be the first non-this parameter in the type system.
	/// There is no way to represent a `this` parameter with a SourceParameterIndex
	/// </summary>
	/// <example>
	/// In a call to a non-static function Foo(int a, int b, int c)
	/// 0 refers to a,
	/// 1 refers to b,
	/// 2 refers to c.
	/// </example>
	public enum SourceParameterIndex
	{ }
}
