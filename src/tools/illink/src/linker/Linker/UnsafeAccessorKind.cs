// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copy of the enum from CoreLib - once that is merged this should be deleted.
namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Specifies the kind of target to which an <see cref="UnsafeAccessorAttribute" /> is providing access.
	/// </summary>
	internal enum UnsafeAccessorKind
	{
		/// <summary>
		/// Provide access to a constructor.
		/// </summary>
		Constructor,

		/// <summary>
		/// Provide access to a method.
		/// </summary>
		Method,

		/// <summary>
		/// Provide access to a static method.
		/// </summary>
		StaticMethod,

		/// <summary>
		/// Provide access to a field.
		/// </summary>
		Field,

		/// <summary>
		/// Provide access to a static field.
		/// </summary>
		StaticField
	};
}
