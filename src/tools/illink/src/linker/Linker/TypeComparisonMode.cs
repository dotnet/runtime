// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker
{
	// Copied from https://github.com/jbevain/cecil/blob/master/Mono.Cecil/TypeComparisonMode.cs
	internal enum TypeComparisonMode
	{
		Exact,
		SignatureOnly,

		/// <summary>
		/// Types can be in different assemblies, as long as the module, assembly, and type names match they will be considered equal
		/// </summary>
		SignatureOnlyLoose
	}
}
